using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Net;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MusicShare.Droid.Services.Impl
{
    abstract class DeviceChannel : DisposableObject, IDeviceChannel
    {
        public System.IO.Stream Stream { get; protected set; }

        public DeviceChannel()
        {
        }
    }

    class BtDeviceChannel : DeviceChannel
    {
        public BtDeviceEntryInfo Info { get; }
        public BtDeviceConnection BtCnn { get; }

        public BtDeviceChannel(BtDeviceConnection btCnn)
        {
            this.BtCnn = btCnn;
            this.Info = new BtDeviceEntryInfo(btCnn.Socket.RemoteDevice.Address, btCnn.Socket.RemoteDevice.Name);
            this.Stream = btCnn.Stream;
        }

        protected override void DisposeImpl()
        {
            this.BtCnn.SafeDispose();
        }
    }

    class LanDeviceChannel : DeviceChannel
    {
        public NetHostInfo Info { get; }
        public NetDeviceConnection NetCnn { get; }

        public LanDeviceChannel(NetDeviceConnection netCnn)
        {
            this.NetCnn = netCnn;
            this.Info = netCnn.Info;
            this.Stream = netCnn.Stream;
        }

        protected override void DisposeImpl()
        {
            this.NetCnn.Stream.SafeDispose();
        }
    }

    //class WanDeviceConnection : DeviceConnection
    //{
    //}

    class PlayerImpl : DisposableObject, IPlayer
    {
        public event Action OnStateChanged;
        public event Action OnPositionChanged;

        public event Action<IDeviceChannel> OnConnection;

        public PlayerState State { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsStopped { get; private set; }
        public TimeSpan CurrentDuration { get; private set; }
        public TimeSpan CurrentPosition { get; private set; }

        // public event Action<StreamDataPacket> OnAudioData;

        readonly Service _service;

        public BluetoothConnector BtConnector { get; }
        public NetworkConnector NetConnector { get; }
        public PlayerPlaylist Playlist { get; }

        IBluetoothConnector IPlayer.BtConnector { get { return this.BtConnector; } }
        INetworkConnector IPlayer.NetConnector { get { return this.NetConnector; } }
        IPlayerPlaylist IPlayer.Playlist { get { return this.Playlist; } }

        LocalAudioPayerTarget _localTarget;
        AsyncMediaStreamPlayerTarget _streamTarget;

        MediaFilePayerSource _fileSource;
        AsyncMediaStreamPlayerSource _streamSource;

        readonly WorkerThreadPool _threadPool = new WorkerThreadPool(3);

        public string LocalDeviceName { get; private set; }

        ConnectivityManager _connectivityManager;

        const ushort LocalPort = 32332;

        readonly object _connectionsLock = new object();
        readonly List<DeviceChannel> _connections = new List<DeviceChannel>();

        public PlayerImpl(Service service)
        {
            _service = service;
            _connectivityManager = ConnectivityManager.FromContext(_service.ApplicationContext);

            this.Playlist = new PlayerPlaylist(service.ApplicationContext);

            this.LocalDeviceName = Android.Provider.Settings.Secure.GetString(service.ContentResolver, "bluetooth_name");

            this.BtConnector = new BluetoothConnector(service);
            this.NetConnector = new NetworkConnector(service, LocalPort, this.LocalDeviceName);

            this.BtConnector.OnNewConnection += btCnn => {
                var chan = new BtDeviceChannel(btCnn);

                lock (_connectionsLock)
                {
                    _connections.Add(chan);
                }

                this.OnConnection?.Invoke(chan);
            };
            this.NetConnector.OnNewConnection += netCnn => {
                var chan = new LanDeviceChannel(netCnn);

                lock (_connectionsLock)
                {
                    _connections.Add(chan);
                }

                this.OnConnection?.Invoke(chan);
            };
        }

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
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Unable to obtain network info:" + ex.ToString());
            }

            try
            {
                info.BluetoothAddress = BluetoothAdapter.DefaultAdapter.Address;
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Unable to obtain bluetooth info:" + ex.ToString());
            }

            return info;
        }

        Thread _connectThread;

        public void Connect(ConnectivityInfoType target, Action callback)
        {
            _connectThread = new Thread(() => {
                bool TryIpEps(IpEndPointInfo[] eps)
                {
                    foreach (var ep in eps ?? new IpEndPointInfo[0])
                        if (this.NetConnector.ConnectTo(ep.Address, (ushort)ep.Port))
                            return true;
                    return false;
                }

                var ok = TryIpEps(target.IpV4EndPoints);
                if (!ok)
                    ok = TryIpEps(target.IpV6EndPoints);

                if (!ok && !String.IsNullOrWhiteSpace(target.BluetoothAddress))
                    ok = this.BtConnector.ConnectBt(target.BluetoothAddress);

                // TODO connect over online service
                callback();
            }) {
                IsBackground = true
            };
            _connectThread.Start();
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
            if (_fileSource != null)
                _fileSource.SafeDispose();
            if (_localTarget != null)
                _localTarget.SafeDispose();
        }

        public void Start()
        {
            if (this.IsStopped)
            {
                this.Cleanup();

                var trackInfo = this.Playlist.Get(this.Playlist.ActiveTrackIndex);
                _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, trackInfo.FilePathOrUri);
                _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);
                
                var prevPosition = TimeSpan.Zero;
                this.CurrentDuration = trackInfo.Duration;
                this.CurrentPosition = TimeSpan.Zero;
                this.OnPositionChanged?.Invoke();

                _fileSource.OnRawData += (f, t, e) => {
                    this.CurrentPosition = t;
                    
                    var dt = t - prevPosition;
                    if (dt.TotalSeconds >= 1)
                    {
                        prevPosition = t;
                        this.OnPositionChanged?.Invoke();
                    }

                    _localTarget.Write(f, e);
                    if (e)
                    {
                        this.Stop();
                        if (this.Playlist.TryAdvance())
                        {
                            this.Start();
                        }
                    }
                };

                _localTarget.Start();
                _fileSource.Start();
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
            if (this.IsPlaying && _fileSource != null)
            {
                _fileSource.Pause();
                this.UpdateState(PlayerState.Paused);
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
        }

        public void JumpToTrack(int index)
        {
            if (!this.IsStopped)
                this.Stop();

            if (this.Playlist.TryActivate(index))
                this.Start();
        }

        public void Test2()
        {

            var filePathOrUri = this.Playlist.Get(this.Playlist.ActiveTrackIndex).FilePathOrUri;
            byte[] buff;
            if (File.Exists(filePathOrUri))
            {
                var stream = File.Open(filePathOrUri, FileMode.Open);
                buff = new byte[stream.Length];
                stream.Read(buff, 0, buff.Length);
            }
            else
            {
                var uri = Android.Net.Uri.Parse(filePathOrUri);
                var stream = _service.ApplicationContext.ContentResolver.OpenInputStream(uri);
                buff = new byte[stream.Length];
                stream.Read(buff, 0, buff.Length);
            }

            _streamSource = new AsyncMediaStreamPlayerSource(new StreamDataHeadPacket() {
                StreamId = 0,
                TotalSize = buff.Length,
                DataFrame = new RawData(buff, 0, buff.Length)
            });
            _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);

            _streamSource.OnRawData += (f, s, t) => {
                _localTarget.Write(f, t);
                if (t)
                {
                    _streamSource.SafeDispose();
                }
            };

            _localTarget.Start();
            _fileSource.Start();
        }

        protected override void DisposeImpl()
        {
            this.Cleanup();
            this.NetConnector.SafeDispose();
            this.BtConnector.SafeDispose();
            this.Playlist.SafeDispose();
        }
    }
}