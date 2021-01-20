using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using MusicShare.Services.Streams;
using MusicShare.Uwp.Services.Bluetooth;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;

namespace MusicShare.Uwp.Services
{
    internal class PlayerPlaylist : IDisposable, IPlayerPlaylist
    {
        public event Action OnClear;
        public event Action<int> OnRemoveItem;
        public event Action<int, PlayerTrackInfo> OnInsertItem;
        public event Action<int> OnActiveItemChanged;
        public event Action<int, PlayerTrackInfo> OnUpdateItem;

        // readonly Context _context;

        private readonly List<PlayerTrackInfo> _playerTracks = new List<PlayerTrackInfo>();

        public int ActiveTrackIndex { get; private set; }
        public int TracksCount { get { return _playerTracks.Count; } }
        public bool IsEmpty { get { return _playerTracks.Count == 0; } }

        public PlayerPlaylist() //Context context)
        {
            // _context = context;
        }

        public void Enumerate()
        {
            this.OnClear?.Invoke();

            for (int i = 0; i < _playerTracks.Count; i++)
            {
                this.OnInsertItem?.Invoke(i, _playerTracks[i]);
            }

            if (_playerTracks.Count > 0)
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
        }

        public PlayerTrackInfo Get(int index)
        {
            return _playerTracks[index];
        }

        public void Add(string filepath)
        {
            var trackInfo = new PlayerTrackInfo(null, null, null, null, Path.GetFileNameWithoutExtension(filepath), filepath); // this.CollectTrackInfo(filepath);
            var index = _playerTracks.Count;
            _playerTracks.Add(trackInfo);
            this.OnInsertItem?.Invoke(index, trackInfo);
        }

        public void Remove(int index)
        {
            _playerTracks.RemoveAt(index);
            this.OnRemoveItem?.Invoke(index);

            {
                var active = this.ActiveTrackIndex;
                if (active == index) // TODO consider this case
                {
                    active = index - 1;
                    this.ActiveTrackIndex = active;
                }
                else if (active > index)
                {
                    active--;
                    this.ActiveTrackIndex = active;
                    this.OnActiveItemChanged?.Invoke(active);
                }
            }
        }

        public void Move(int from, int to)
        {
            var track = _playerTracks[from];
            _playerTracks.RemoveAt(from);
            this.OnRemoveItem?.Invoke(from);

            var actualTo = to > from ? to - 1 : to;
            _playerTracks.Insert(actualTo, track);
            this.OnInsertItem?.Invoke(actualTo, track);

            {
                var active = this.ActiveTrackIndex;
                if (active == from)
                    active = to;
                else if (active > from)
                    active--;

                if (active >= actualTo)
                    active++;

                this.ActiveTrackIndex = active;
                this.OnActiveItemChanged?.Invoke(active);
            }
        }

        public bool TryRevert()
        {
            if (this.ActiveTrackIndex - 1 >= 0)
            {
                this.ActiveTrackIndex--;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryAdvance()
        {
            if (this.ActiveTrackIndex + 1 < _playerTracks.Count)
            {
                this.ActiveTrackIndex++;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryActivate(int index)
        {
            if (index >= 0 && index < _playerTracks.Count)
            {
                this.ActiveTrackIndex = index;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            _playerTracks.Clear();
            this.ActiveTrackIndex = 0;
            this.OnClear?.Invoke();
            this.OnActiveItemChanged?.Invoke(0);
        }

        //private PlayerTrackInfo CollectTrackInfo(string filePathOrUri)
        //{
        //    var metadataRetriever = new MediaMetadataRetriever();

        //    if (File.Exists(filePathOrUri))
        //    {
        //        metadataRetriever.SetDataSource(filePathOrUri);
        //    }
        //    else
        //    {
        //        var uri = Android.Net.Uri.Parse(filePathOrUri);
        //        metadataRetriever.SetDataSource(_context, uri);
        //    }

        //    var album = metadataRetriever.ExtractMetadata(MetadataKey.Album);

        //    var artists = new[] {
        //        metadataRetriever.ExtractMetadata(MetadataKey.Albumartist),
        //        metadataRetriever.ExtractMetadata(MetadataKey.Artist),
        //        metadataRetriever.ExtractMetadata(MetadataKey.Author),
        //        metadataRetriever.ExtractMetadata(MetadataKey.Composer),
        //        metadataRetriever.ExtractMetadata(MetadataKey.Writer),
        //    };
        //    var artist = artists.FirstOrDefault(s => !string.IsNullOrEmpty(s));

        //    var trackNumberStr = metadataRetriever.ExtractMetadata(MetadataKey.CdTrackNumber);
        //    var trackNumber = !string.IsNullOrEmpty(trackNumberStr) && int.TryParse(trackNumberStr, out var trackNumberResult) ? trackNumberResult : default(int?);

        //    var durationStr = metadataRetriever.ExtractMetadata(MetadataKey.Duration);
        //    var duration = !string.IsNullOrEmpty(durationStr) && int.TryParse(durationStr, out var durationResult) ? TimeSpan.FromMilliseconds(durationResult) : default(TimeSpan?);

        //    var title = metadataRetriever.ExtractMetadata(MetadataKey.Title);

        //    metadataRetriever.SafeDispose();

        //    return new PlayerTrackInfo(album, trackNumber, artist, duration, title, filePathOrUri);
        //}

        public void Dispose()
        {
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    internal class PlayerImpl : IDisposable, IPlayer
    {
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

        //private readonly Service _service;
        private readonly StreamNegotiator _negotiator;

        public IBluetoothConnector BtConnector { get; }
        public INetworkConnector NetConnector { get; }
        public PlayerPlaylist Playlist { get; }

        IPlayerPlaylist IPlayer.Playlist { get { return this.Playlist; } }

        //private LocalAudioPayerTarget _localTarget;
        //private AsyncMediaStreamPlayerTarget _streamTarget;
        //private MediaFilePayerSource _fileSource;
        //private AsyncMediaStreamPlayerSource _streamSource;
        //private readonly WorkerThreadPool _threadPool = new WorkerThreadPool(3);

        public string LocalDeviceName { get; private set; }

        // private ConnectivityManager _connectivityManager;
        private const ushort LocalPort = 32332;
        private readonly object _connectionsLock = new object();
        private readonly List<IDeviceChannel> _connections = new List<IDeviceChannel>();

        public PlayerImpl() // Service service)
        {
            this.UpdateState(PlayerState.Stopped);

            //_service = service;
            // _connectivityManager = ConnectivityManager.FromContext(_service.ApplicationContext);

            this.Playlist = new PlayerPlaylist(); // service.ApplicationContext);

            this.LocalDeviceName = this.ObtainLocalDeviceName();

            _negotiator = new StreamNegotiator();
            var netConnector = new NetworkConnectorService(_negotiator, PlayerService.Id, LocalPort, this.LocalDeviceName);
            //var btConnector = BluetoothAdapter.DefaultAdapter != null ? new BluetoothConnectorService(_negotiator, PlayerService.Id, this.LocalDeviceName, service)
            //                                                           : (IBluetoothConnectorImpl)new NullBluetoothConnectorService();

            Windows.Devices.Bluetooth.BluetoothAdapter adapter;
            try { adapter = Windows.Devices.Bluetooth.BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult(); }
            catch (Exception ex) { Debug.Print(ex.ToString()); adapter = null; }
            var btConnector = adapter != null ? new Bluetooth.BluetoothConnectorService(adapter, _negotiator, PlayerService.Id, this.LocalDeviceName)
                                              : (IBluetoothConnectorImpl)new NullBluetoothConnectorService();

            this.NetConnector = netConnector;
            this.BtConnector = btConnector;

            this.BtConnector.OnConnection += chan => {
                this.RegisterConnection(chan);
            };
            this.NetConnector.OnConnection += chan => {
                this.RegisterConnection(chan);
            };

            netConnector.Start();
            btConnector.Start();
            _negotiator.Start();
        }

        private void RegisterConnection(IDeviceChannel chan)
        {
            lock (_connectionsLock)
            {
                _connections.Add(chan);
            }

            this.OnConnection?.Invoke(chan);


            this.InitAudioGraph(chan);

            chan.Start();
            chan.StartListening();
        }

        private String ObtainLocalDeviceName()
        {
            var envMachineName = System.Environment.MachineName;
            //if (BluetoothAdapter.DefaultAdapter != null)
            //    return Android.Provider.Settings.Secure.GetString(_service.ContentResolver, "bluetooth_name");

            //try
            //{
            //    var javaString = new Java.Lang.String();
            //    var systemPropertiesClass = Class.ForName("android.os.SystemProperties");
            //    var getter = systemPropertiesClass.GetMethod("get", javaString.Class);

            //    var obj = getter.Invoke(javaString, "ro.product.device");
            //    if (obj != null)
            //    {
            //        var str = obj.ToString();
            //        if (!string.IsNullOrWhiteSpace(str))
            //            return str;
            //    }
            //}
            //catch (Exception e)
            //{
            //}

            return envMachineName;
        }


        private readonly HashSet<string> _authTokens = new HashSet<string>();

        public ConnectivityInfoType GetConnectivityInfo()
        {
            //var info = new ConnectivityInfoType();
            //info.DeviceName = this.LocalDeviceName;
            //try
            //{
            //    var v4list = new List<IpEndPointInfo>();
            //    var v6list = new List<IpEndPointInfo>();
            //    foreach (var intf in NetworkInterface.NetworkInterfaces.Of<NetworkInterface>())
            //    {
            //        foreach (var addr in intf.InetAddresses.Of<InetAddress>())
            //        {
            //            if (!addr.IsLoopbackAddress)
            //            {
            //                String sAddr = addr.HostAddress;
            //                //boolean isIPv4 = InetAddressUtils.isIPv4Address(sAddr);
            //                var isIPv4 = sAddr.IndexOf(':') < 0;

            //                if (isIPv4)
            //                {
            //                    v4list.Add(new IpEndPointInfo() { Address = sAddr, Port = LocalPort });
            //                }
            //                else
            //                {
            //                    int delim = sAddr.IndexOf('%'); // drop ip6 zone suffix
            //                    var v6addr = delim < 0 ? sAddr.ToUpper() : sAddr.Substring(0, delim).ToUpper();
            //                    v6list.Add(new IpEndPointInfo() { Address = v6addr, Port = LocalPort });
            //                }
            //            }
            //        }
            //    }
            //    info.IpV4EndPoints = v4list.ToArray();
            //    info.IpV6EndPoints = v6list.ToArray();

            //    var token = Guid.NewGuid().ToString();
            //    _authTokens.Add(token);
            //    info.AuthToken = token;
            //}
            //catch (Exception ex)
            //{
            //    Log.TraceMethod("Unable to obtain network info:" + ex.ToString());
            //}

            //try
            //{
            //    info.BluetoothAddress = BluetoothAdapter.DefaultAdapter?.Address ?? null;
            //}
            //catch (Exception ex)
            //{
            //    Log.TraceMethod("Unable to obtain bluetooth info:" + ex.ToString());
            //}

            return new ConnectivityInfoType();
        }

        // private Thread _connectThread;

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
            //if (_fileSource != null)
            //    _fileSource.SafeDispose();
            //if (_localTarget != null)
            //    _localTarget.SafeDispose();
        }

        // IPlayerTarget _target; // TODO: SEEMS IT WORKS


        private AudioGraph audioGraph;
        private AudioFileInputNode fileInputNode;
        private MediaSourceAudioInputNode streamMediaSourceNode;
        private AudioDeviceOutputNode deviceOutputNode;
        private Task task;
        private Thread _thread;

        private void InitAudioGraph(IDeviceChannel chan)
        {
            void PostSample(MediaStreamSourceSampleRequest req, StampedStreamDataPacket packet)
            {
                var data = packet.DataFrame;
                var flags = (StampedStreamDataPacketFlags)packet.Flags;

                var buffer = new Windows.Storage.Streams.Buffer((uint)data.Size);
                data.Data.AsBuffer(data.Offset, data.Size);
                var sample = MediaStreamSample.CreateFromBuffer(buffer, packet.Stamp);
                sample.KeyFrame = flags.HasFlag(StampedStreamDataPacketFlags.KeyFrame);
                req.Sample = sample;
                req.GetDeferral().Complete();
            }

            var locker = new object();
            var queue = new Queue<StampedStreamDataPacket>();
            MediaStreamSourceSampleRequest request = null;

            chan.OnStreamData += pckt => {
                if (pckt is StampedStreamDataHeadPacket head)
                {
                    _thread = new Thread(() => {
                        AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);

                        CreateAudioGraphResult result = AudioGraph.CreateAsync(settings).GetAwaiter().GetResult();
                        if (result.Status != AudioGraphCreationStatus.Success)
                        {
                            //ShowErrorMessage("AudioGraph creation error: " + result.Status.ToString());
                            return;
                        }

                        audioGraph = result.Graph;

                        CreateAudioDeviceOutputNodeResult result2 = audioGraph.CreateDeviceOutputNodeAsync().GetAwaiter().GetResult();

                        if (result2.Status != AudioDeviceNodeCreationStatus.Success)
                        {
                            // Cannot create device output node
                            return;
                        }

                        deviceOutputNode = result2.DeviceOutputNode;
                        var streamDescriptor = new AudioStreamDescriptor(AudioEncodingProperties.CreateAac((uint)head.SampleRate, head.IsMono ? 1u : 2u, 64 * 1024));
                        var streamSource = new MediaStreamSource(streamDescriptor) { IsLive = true };
                        streamSource.Starting += (sender, ea) => { ea.Request.GetDeferral().Complete(); };
                        streamSource.SampleRendered += (sender, ea) => { };
                        streamSource.Closed += (sender, ea) => { };
                        streamSource.SwitchStreamsRequested += (sender, ea) => { ea.Request.GetDeferral().Complete(); };
                        streamSource.SampleRequested += (sender, ea) => {
                            lock (locker)
                            {
                                if (queue.Count > 0)
                                {
                                    PostSample(ea.Request, queue.Dequeue());
                                }
                                else
                                {
                                    ea.Request.ReportSampleProgress(1);
                                    request = ea.Request;
                                }
                            }
                        };

                        var mediaSource = MediaSource.CreateFromMediaStreamSource(streamSource);
                        var op = audioGraph.CreateMediaSourceAudioInputNodeAsync(mediaSource);
                        var tt = op.AsTask();

                        while (!tt.IsCompleted)
                        {
                            Thread.Sleep(100);
                        }

                        var awaiter = tt.GetAwaiter();
                        var result3 = awaiter.GetResult();
                        if (result3.Status != MediaSourceAudioInputNodeCreationStatus.Success)
                        {
                            // Cannot create device output node
                            return;
                        }

                        var streamMediaSourceNode = result3.Node;

                        streamMediaSourceNode.AddOutgoingConnection(deviceOutputNode);

                        streamMediaSourceNode.MediaSourceCompleted += (sender, ea) => {
                        };

                        audioGraph.Start();
                    });
                    _thread.Start();
                }
                else
                {
                    //if (streamMediaSourceNode != null)
                    {
                        lock (locker)
                        {
                            if (request != null)
                            {
                                if (queue.Count == 0)
                                {
                                    PostSample(request, pckt);
                                }
                                else 
                                {
                                    queue.Enqueue(pckt);
                                    PostSample(request, queue.Dequeue());
                                }
                                request = null;
                            }
                            else
                            {
                                queue.Enqueue(pckt);
                            }
                        }
                    }
                }
            };
        }

        private void InitAudioGraph(string filePath)
        {
            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);

            CreateAudioGraphResult result = AudioGraph.CreateAsync(settings).GetAwaiter().GetResult();
            if (result.Status != AudioGraphCreationStatus.Success)
            {
                //ShowErrorMessage("AudioGraph creation error: " + result.Status.ToString());
                return;
            }

            audioGraph = result.Graph;

            CreateAudioDeviceOutputNodeResult result2 = audioGraph.CreateDeviceOutputNodeAsync().GetAwaiter().GetResult();

            if (result2.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
                return;
            }

            deviceOutputNode = result2.DeviceOutputNode;

            if (audioGraph == null)
                return;

            //FileOpenPicker filePicker = new FileOpenPicker();
            //filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            //filePicker.FileTypeFilter.Add(".mp3");
            //filePicker.FileTypeFilter.Add(".wav");
            //filePicker.FileTypeFilter.Add(".wma");
            //filePicker.FileTypeFilter.Add(".m4a");
            //filePicker.ViewMode = PickerViewMode.Thumbnail;
            //StorageFile file = await filePicker.PickSingleFileAsync();
            var storageFile = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath).GetAwaiter().GetResult();
            CreateAudioFileInputNodeResult result3 = audioGraph.CreateFileInputNodeAsync(storageFile).GetAwaiter().GetResult();

            if (result3.Status != AudioFileNodeCreationStatus.Success)
            {
            }

            fileInputNode = result3.FileInputNode;

            fileInputNode.AddOutgoingConnection(deviceOutputNode);

            audioGraph.Start();
        }

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

                ////_fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, trackInfo.FilePathOrUri);
                ////_localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);

                ////// _target = PlayerTarget.ToAsyncMediaStream(0, _fileSource.SampleRateHz, _fileSource.IsMono);

                ////var prevPosition = TimeSpan.Zero;
                ////this.CurrentDuration = trackInfo.Duration ?? TimeSpan.MaxValue;
                ////this.CurrentPosition = TimeSpan.Zero;
                ////this.OnPositionChanged?.Invoke();

                ////_fileSource.OnRawData += (f, t, e) => {
                ////    this.CurrentPosition = t;

                ////    var dt = t - prevPosition;
                ////    if (dt.TotalSeconds >= 1 && !e)
                ////    {
                ////        prevPosition = t;
                ////        this.OnPositionChanged?.Invoke();
                ////    }

                ////    _localTarget.Write(f, e);
                ////    // _target.Write(f, e);
                ////    if (e)
                ////    {
                ////        this.PlayNextTrack();
                ////    }
                ////};

                ////_localTarget.Start();
                ////// _target.Start();
                ////_fileSource.Start();
                ///
                this.InitAudioGraph(trackInfo.FilePathOrUri);

                this.UpdateState(PlayerState.Playing);
            }
            else if (this.IsPaused)
            {
                /////_fileSource.Resume();
                this.UpdateState(PlayerState.Playing);
            }
        }

        public void Pause()
        {
            //if (this.IsPlaying && _fileSource != null)
            //{
            //    _fileSource.Pause();
            //    this.UpdateState(PlayerState.Paused);
            //}
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
            //_localTarget.SetVolume(volume);
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

        public void Dispose()
        {
            this.Cleanup();
            this.NetConnector.SafeDispose();
            this.BtConnector.SafeDispose();
            this.Playlist.SafeDispose();
        }
    }

    internal class PlayerService : IPlayerService
    {
        public static readonly Guid Id = Guid.Parse("B2C68981-2ACD-4622-950E-FAF0E10ADE11");
        public const string Name = "com.companyname.MusicShare.Droid.Services.PlayerService";

        public IPlayer Player { get; }

        public PlayerService()
        {
            this.Player = new PlayerImpl();

            ServiceContext.SetInstance(this);
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
