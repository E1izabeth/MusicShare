using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang.Reflect;
using Java.Net;
using MusicShare.Droid.Services.Bluetooth;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using MusicShare.Services.Streams;
using Class = Java.Lang.Class;

namespace MusicShare.Droid.Services.Impl
{
    internal class PlayerImpl : DisposableObject, IPlayer
    {
        private class ChanHolder
        {
            public LinkedListNode<ChanHolder> ConnectionsListNode { get; set; }
            public LinkedListNode<ChanHolder> ListenersListNode { get; set; }

            public IDeviceChannel Channel { get; }

            public ChanHolder(IDeviceChannel channel)
            {
                this.Channel = channel;
            }
        }

        public event Action OnStateChanged;
        public event Action OnPositionChanged;

        public event Action<IDeviceChannel> OnConnection;
        public event Action<int> OnActiveItemChanged;

        public PlayerState State { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsStopped { get; private set; }
        public TimeSpan CurrentDuration { get; private set; }
        public TimeSpan CurrentPosition { get; private set; }

        // public event Action<StreamDataPacket> OnAudioData;

        private readonly Service _service;
        private readonly StreamNegotiator _negotiator;

        public IBluetoothConnector BtConnector { get; }
        public INetworkConnector NetConnector { get; }
        public PlayerPlaylist Playlist { get; }

        IPlayerPlaylist IPlayer.Playlist { get { return this.Playlist; } }

        private MediaFilePayerSource _fileSource;
        private LocalAudioPayerTarget _localTarget;
        private volatile AsyncMediaStreamPlayerSource _streamSource;
        private volatile AsyncMediaStreamPlayerTarget _streamTarget;
        private readonly WorkerThreadPool _threadPool = new WorkerThreadPool(3);

        public string LocalDeviceName { get; private set; }

        private readonly ConnectivityManager _connectivityManager;
        private const ushort LocalPort = 32332;
        private readonly object _connectionsLock = new object();
        private readonly LinkedList<ChanHolder> _connections = new LinkedList<ChanHolder>();

        private readonly object _activeListenersLock = new object();
        private readonly LinkedList<ChanHolder> _activeListeners = new LinkedList<ChanHolder>();

        public PlayerImpl(Service service)
        {
            this.UpdateState(PlayerState.Stopped);

            _service = service;
            _connectivityManager = ConnectivityManager.FromContext(_service.ApplicationContext);

            this.Playlist = new PlayerPlaylist(service.ApplicationContext);

            this.LocalDeviceName = this.ObtainLocalDeviceName();

            _negotiator = new StreamNegotiator();
            var netConnector = new NetworkConnectorService(_negotiator, PlayerService.Id, LocalPort, this.LocalDeviceName);
            var btConnector = BluetoothAdapter.DefaultAdapter != null ? new BluetoothConnectorService(_negotiator, PlayerService.Id, this.LocalDeviceName, service)
                                                                      : (IBluetoothConnectorImpl)new NullBluetoothConnectorService();
            this.NetConnector = netConnector;
            this.BtConnector = btConnector;

            this.BtConnector.OnConnection += chan => this.RegisterConnection(chan);
            this.NetConnector.OnConnection += chan => this.RegisterConnection(chan);

            netConnector.Start();
            btConnector.Start();
            _negotiator.Start();
        }

        private void RegisterConnection(IDeviceChannel chan)
        {
            var holder = new ChanHolder(chan);

            lock (_connectionsLock)
            {
                holder.ConnectionsListNode = _connections.AddLast(holder);
            }

            chan.OnEnabled += () => {
                if (holder.ListenersListNode == null)
                {
                    lock (_connectionsLock)
                    {
                        lock (_activeListenersLock)
                        {
                            holder.ListenersListNode = _activeListeners.AddLast(holder);

                            if (_streamTarget == null)
                            {
                                _streamTarget = new AsyncMediaStreamPlayerTarget(0, 44100, false);
                                _streamTarget.OnData += pckt => {
                                    lock (_activeListenersLock)
                                    {
                                        foreach (var item in _activeListeners)
                                        {
                                            item.Channel.SendData(pckt);
                                        }
                                    }
                                };
                                _streamTarget.Start();
                            }
                        }
                    }
                }
            };
            chan.OnDisabled += () => {
                if (holder.ListenersListNode != null)
                {
                    lock (_connectionsLock)
                    {
                        lock (_activeListenersLock)
                        {
                            _activeListeners.Remove(holder.ListenersListNode);

                            if (_activeListeners.Count == 0)
                            {
                                _streamTarget.SafeDispose();
                                _streamTarget = null;
                            }
                        }
                    }
                }
            };
            chan.OnClosed += () => {
                lock (_connectionsLock)
                {
                    if (holder.ListenersListNode != null)
                    {
                        lock (_activeListenersLock)
                        {
                            if (holder.ListenersListNode != null)
                            {
                                _activeListeners.Remove(holder.ListenersListNode);
                            }
                        }
                    }

                    _connections.Remove(holder.ConnectionsListNode);
                    holder.ConnectionsListNode = null;
                }
            };

            this.OnConnection?.Invoke(chan);

            this.Playlist.Add(chan);

            chan.Start();
        }

        private String ObtainLocalDeviceName()
        {
            var envMachineName = System.Environment.MachineName;
            if (BluetoothAdapter.DefaultAdapter != null)
                return Android.Provider.Settings.Secure.GetString(_service.ContentResolver, "bluetooth_name");

            try
            {
                var javaString = new Java.Lang.String();
                var systemPropertiesClass = Class.ForName("android.os.SystemProperties");
                var getter = systemPropertiesClass.GetMethod("get", javaString.Class);

                var obj = getter.Invoke(javaString, "ro.product.device");
                if (obj != null)
                {
                    var str = obj.ToString();
                    if (!string.IsNullOrWhiteSpace(str))
                        return str;
                }
            }
            catch (Exception e)
            {
            }

            return envMachineName;
        }


        private readonly HashSet<string> _authTokens = new HashSet<string>();

        public ConnectivityInfoType GetConnectivityInfo()
        {
            var info = new ConnectivityInfoType();
            info.DeviceName = this.LocalDeviceName;
            try
            {
                var v4list = new List<IpEndPointInfo>();
                var v6list = new List<IpEndPointInfo>();
                foreach (var intf in NetworkInterface.NetworkInterfaces.Of<NetworkInterface>())
                {
                    foreach (var addr in intf.InetAddresses.Of<InetAddress>())
                    {
                        if (!addr.IsLoopbackAddress)
                        {
                            String sAddr = addr.HostAddress;
                            //boolean isIPv4 = InetAddressUtils.isIPv4Address(sAddr);
                            var isIPv4 = sAddr.IndexOf(':') < 0;

                            if (isIPv4)
                            {
                                v4list.Add(new IpEndPointInfo() { Address = sAddr, Port = LocalPort });
                            }
                            else
                            {
                                int delim = sAddr.IndexOf('%'); // drop ip6 zone suffix
                                var v6addr = delim < 0 ? sAddr.ToUpper() : sAddr.Substring(0, delim).ToUpper();
                                v6list.Add(new IpEndPointInfo() { Address = v6addr, Port = LocalPort });
                            }
                        }
                    }
                }
                info.IpV4EndPoints = v4list.ToArray();
                info.IpV6EndPoints = v6list.ToArray();

                var token = Guid.NewGuid().ToString();
                _authTokens.Add(token);
                info.AuthToken = token;
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Unable to obtain network info:" + ex.ToString());
            }

            try
            {
                info.BluetoothAddress = BluetoothAdapter.DefaultAdapter?.Address ?? null;
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Unable to obtain bluetooth info:" + ex.ToString());
            }

            return info;
        }

        public void Connect(ConnectivityInfoType target, Action<bool> callback)
        {
            IEnumerator<IpEndPointInfo> ips = (target.IpV4EndPoints ?? new IpEndPointInfo[0]).Concat(target.IpV6EndPoints ?? new IpEndPointInfo[0]).GetEnumerator();

            void onNetOk(NetDeviceChannel cnn) { callback(true); }
            void tryNextNet(Exception ex)
            {
                if (ips.MoveNext())
                    this.NetConnector.Connect(ips.Current.Address, (ushort)ips.Current.Port, onNetOk, tryNextNet);
                else
                    this.BtConnector.Connect(target.BluetoothAddress, cnn => callback(true), ex2 => callback(false));
            }

            tryNextNet(null);
        }

        private void UpdateState(PlayerState newState)
        {
            this.State = newState;
            this.IsPlaying = newState == PlayerState.Playing;
            this.IsStopped = newState == PlayerState.Stopped;
            this.IsPaused = newState == PlayerState.Paused;
            this.OnStateChanged?.Invoke();
        }

        private void Cleanup()
        {
            if (_streamSource != null)
                _streamSource.SafeDispose();
            if (_fileSource != null)
                _fileSource.SafeDispose();
            if (_localTarget != null)
                _localTarget.SafeDispose();
            //if (_streamTarget != null)
            //    _streamTarget.SafeDispose();
        }

        private IPlayerTarget _target; // TODO: SEEMS IT WORKS

        public void Start()
        {
            if (this.Playlist.IsEmpty)
                return;

            if (this.IsStopped)
            {
                this.Cleanup();

                var trackIndex = this.Playlist.ActiveTrackIndex;
                if (trackIndex < 0 || trackIndex >= this.Playlist.TracksCount)
                    if (!this.Playlist.TryActivate(trackIndex = 0))
                        return;

                var trackInfo = this.Playlist.Get(trackIndex);

                this.Playlist.TryActivate(trackIndex);

                if (trackInfo.Channel != null)
                {
                    var start = DateTime.Now;
                    var prevPosition = TimeSpan.Zero;

                    var chan = trackInfo.Channel;
                    chan.StartListening();
                    chan.OnStreamData += pckt => {
                        if (pckt is StampedStreamDataHeadPacket head)
                        {
                            if (_streamSource != null)
                                _streamSource.SafeDispose();
                            if (_localTarget != null)
                                _localTarget.SafeDispose();

                            start = DateTime.Now;
                            _streamSource = PlayerSource.FromMediaStream(head);
                            _localTarget = PlayerTarget.ToLocalAudio(head.SampleRate, head.IsMono);

                            _streamSource.OnRawData += (f, t, e) => {
                                this.CurrentPosition = t;

                                var dt = t - prevPosition;
                                if (dt.TotalSeconds >= 1 && !e)
                                {
                                    prevPosition = t;
                                    this.OnPositionChanged?.Invoke();
                                }

                                _localTarget.Write(f, e);

                                if (_streamTarget != null)
                                    _streamTarget.Write(f, e);

                                // _target.Write(f, e);
                                if (e)
                                {
                                    this.PlayNextTrack();
                                }

                            };

                            _localTarget.Start();
                            _streamSource.Start();
                        }
                        else if (_streamSource != null)
                        {
                            _streamSource.PushStreamData(pckt);
                        }

                        if (!(pckt is StampedStreamDataTailPacket))
                        {
                            this.CurrentPosition = pckt.Stamp;
                            this.CurrentDuration = DateTime.Now - start;

                            var dt = pckt.Stamp - prevPosition;
                            if (dt.TotalSeconds >= 1)
                            {
                                prevPosition = pckt.Stamp;
                                this.OnPositionChanged?.Invoke();
                            }
                        }
                    };
                    chan.OnClosed += () => {
                        this.Stop();
                    };
                }
                else
                {
                    _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, trackInfo.FilePathOrUri);
                    _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);

                    // _target = PlayerTarget.ToAsyncMediaStream(0, _fileSource.SampleRateHz, _fileSource.IsMono);

                    var prevPosition = TimeSpan.Zero;
                    this.CurrentDuration = trackInfo.Duration ?? TimeSpan.MaxValue;
                    this.CurrentPosition = TimeSpan.Zero;
                    this.OnPositionChanged?.Invoke();

                    _fileSource.OnRawData += (f, t, e) => {
                        this.CurrentPosition = t;

                        var dt = t - prevPosition;
                        if (dt.TotalSeconds >= 1 && !e)
                        {
                            prevPosition = t;
                            this.OnPositionChanged?.Invoke();
                        }

                        _localTarget.Write(f, e);

                        if (_streamTarget != null)
                            _streamTarget.Write(f, e);

                        // _target.Write(f, e);
                        if (e)
                        {
                            this.PlayNextTrack();
                        }
                    };

                    _localTarget.Start();
                    // _target.Start();
                    _fileSource.Start();
                }

                this.UpdateState(PlayerState.Playing);
            }
            else if (this.IsPaused)
            {
                _fileSource.Resume();
                this.UpdateState(PlayerState.Playing);
            }
        }

        public void Pause()
        {
            if (this.IsPlaying)
            {
                if (_fileSource != null)
                {
                    _fileSource.Pause();
                    this.UpdateState(PlayerState.Paused);
                }

                if (_streamSource != null)
                    this.Stop();
            }
        }

        public void Stop()
        {
            this.Cleanup();
            this.UpdateState(PlayerState.Stopped);
        }

        //    long cnt = 0;
        //    bool start = false;
        //    try
        //    {
        //        _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, this.Playlist.Get(0).FilePathOrUri);
        //        _streamTarget = PlayerTarget.ToAsyncMediaStream(0, _fileSource.SampleRateHz, _fileSource.IsMono);

        //        _fileSource.OnRawData += (f, t) => _streamTarget.Write(f, t);
        //        _streamTarget.OnData += (p) => {

        //            if (p is StreamDataHeadPacket h)
        //            {
        //                _streamSource = PlayerSource.FromMediaStream(h);
        //                _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);
        //                _streamSource.OnRawData += (f, t) => _localTarget.Write(f, t);

        //                _localTarget.Start();
        //                _streamSource.Start();
        //            }
        //            else
        //            {
        //                _streamSource.PushStreamData(p);
        //            }
        //            Log.TraceMethod($"got {p.DataFrame.Size} bytes");
        //            cnt += p.DataFrame.Size;

        //            if (cnt > 2048 && !start)
        //            {
        //                start = true;
        //                _streamSource.Start();
        //            }
        //        };

        //        _streamTarget.Start();
        //        _fileSource.Start();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "FAIL");
        //    }

        public void SetVolume(float volume)
        {
            _localTarget.SetVolume(volume);
        }

        public void PlayNextTrack()
        {
            if (!this.IsStopped)
                this.Stop();

            if (this.Playlist.TryAdvance())
                this.Start();
        }

        public void PlayPrevTrack()
        {
            if (!this.IsStopped)
                this.Stop();

            if (this.Playlist.TryRevert())
                this.Start();

            // this.Test2();
        }

        public void JumpToTrack(int index)
        {
            if (!this.IsStopped)
                this.Stop();

            if (this.Playlist.TryActivate(index))
                this.Start();
        }

        //public void Test2()
        //{

        //    var filePathOrUri = this.Playlist.Get(this.Playlist.ActiveTrackIndex).FilePathOrUri;
        //    byte[] buff;
        //    if (File.Exists(filePathOrUri))
        //    {
        //        var stream = File.Open(filePathOrUri, FileMode.Open);
        //        buff = new byte[stream.Length];
        //        stream.Read(buff, 0, buff.Length);
        //    }
        //    else
        //    {
        //        var uri = Android.Net.Uri.Parse(filePathOrUri);
        //        var stream = _service.ApplicationContext.ContentResolver.OpenInputStream(uri);
        //        buff = new byte[stream.Length];
        //        stream.Read(buff, 0, buff.Length);
        //    }

        //    _streamSource = new AsyncMediaStreamPlayerSource(new StampedStreamDataHeadPacket() {
        //        StreamId = 0,
        //        TotalSize = buff.Length,
        //        DataFrame = new RawData(buff, 0, buff.Length)
        //    });
        //    _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);

        //    _streamSource.OnRawData += (f, s, t) => {
        //        _localTarget.Write(f, t);
        //        if (t)
        //        {
        //            _streamSource.SafeDispose();
        //        }
        //    };

        //    _localTarget.Start();
        //    _fileSource.Start();
        //}

        protected override void DisposeImpl()
        {
            this.Cleanup();
            this.NetConnector.SafeDispose();
            this.BtConnector.SafeDispose();
            this.Playlist.SafeDispose();
        }
    }
}