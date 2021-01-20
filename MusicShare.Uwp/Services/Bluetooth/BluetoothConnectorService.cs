using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Streams;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MusicShare.Uwp.Services.Bluetooth
{

    internal class BluetoothConnectorService : ServiceWorker<BluetoothServieCommand, IBtSvcCmdHandler>, IBtSvcCmdHandler, IBluetoothConnector, IBluetoothConnectorImpl
    {
        //private class BtDiscoveryIntentReceiver : BroadcastReceiver
        //{
        //    public event Action<BluetoothDevice> OnDeviceDiscover;
        //    public event Action OnDiscoveryFinished;
        //    public event Action OnBtActivated;
        //    public event Action OnBtDeactivated;

        //    public BtDiscoveryIntentReceiver()
        //    {
        //    }

        //    public override void OnReceive(Context context, Intent intent)
        //    {
        //        switch (intent.Action)
        //        {
        //            case BluetoothDevice.ActionFound:
        //                {
        //                    var extra = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
        //                    if (extra is BluetoothDevice device)
        //                    {
        //                        this.OnDeviceDiscover?.Invoke(device);
        //                    }
        //                }
        //                break;
        //            case BluetoothAdapter.ActionDiscoveryFinished:
        //                {
        //                    this.OnDiscoveryFinished?.Invoke();
        //                }
        //                break;
        //            case BluetoothAdapter.ActionStateChanged:
        //                {
        //                    // var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
        //                    // Toast.MakeText(context, "BluetoothDevice:" + device.Name + "Changed", ToastLength.Short).Show();
        //                    var blueState = (State)intent.GetIntExtra(BluetoothAdapter.ExtraState, 0);
        //                    switch (blueState)
        //                    {
        //                        case State.On:
        //                            this.OnBtActivated?.Invoke();
        //                            break;
        //                        case State.TurningOff:
        //                        case State.Off:
        //                            this.OnBtDeactivated?.Invoke();
        //                            break;
        //                    }
        //                }
        //                break;
        //            default: // do nothing
        //                break;
        //        }
        //    }
        //}

        private class BtListener : DisposableObject
        {
            public event Action<StreamSocket> OnConnection;
            // public event Action<Exception> OnError;

            private readonly RfcommServiceId _serviceId;
            private readonly RfcommServiceProvider _serviceProvider;
            private readonly StreamSocketListener _socketListener;

            public BtListener(Guid serviceGuid)
            {
                _serviceId = RfcommServiceId.FromUuid(serviceGuid);
                // _adapter = adapter;
                // _listener = adapter.ListenUsingRfcommWithServiceRecord(PlayerService.Name, serviceId);

                _serviceProvider = RfcommServiceProvider.CreateAsync(_serviceId).GetAwaiter().GetResult();

                _socketListener = new StreamSocketListener();
                _socketListener.ConnectionReceived += (sender, ea) => {
                    this.OnConnection?.Invoke(ea.Socket);
                };
            }

            public void Start()
            {
                _socketListener.BindServiceNameAsync(_serviceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication).GetAwaiter().GetResult();
            }

            protected override void DisposeImpl()
            {
                try { _serviceProvider.StopAdvertising(); }
                catch (Exception ex)
                {
                    Log.Error(ex, "Stopping BtListener");
                }

                _socketListener.SafeDispose();
            }
        }

        private class BtDeviceEntry
        {
            public BluetoothDevice Device { get; }
            // public IReadOnlyList<RfcommDeviceService> Services { get; }
            public BtDeviceEntryInfo Info { get; }

            // public BtDeviceEntry(BluetoothDevice device, IReadOnlyList<RfcommDeviceService> services, BtDeviceEntryInfo info)
            public BtDeviceEntry(BluetoothDevice device, BtDeviceEntryInfo info)
            {
                this.Device = device;
                // this.Services = services;
                this.Info = info;
            }
        }

        protected override IBtSvcCmdHandler Interpreter { get { return this; } }

        public event Action<IBtDeviceChannel> OnConnection;
        public event Action<BtDeviceEntryInfo> OnDiscover;
        public event Action<bool> OnStateChanged;
        public event Action<Exception> OnError;

        private readonly StreamNegotiator _negotiator;
        private readonly Guid _serviceGuid;
        private readonly string _name;
        private readonly Dictionary<string, BtDeviceEntry> _knownDevicesByAddress = new Dictionary<string, BtDeviceEntry>(StringComparer.InvariantCultureIgnoreCase);
        //private readonly BtDiscoveryIntentReceiver _btDiscoveryIntentReceiver;
        //private readonly Service _service;
        private readonly BluetoothAdapter _adapter;
        private readonly Radio _radio;

        private DeviceWatcher _watcher = null;
        private BtListener _listener = null;

        private bool _wasEnabled = false;
        public bool IsEnabled { get; private set; } = false;

        public BluetoothConnectorService(BluetoothAdapter adapter, StreamNegotiator negotiator, Guid serviceId, string localName)
        {
            _adapter = adapter;
            _radio = adapter.GetRadioAsync().GetAwaiter().GetResult();
            _negotiator = negotiator;
            _serviceGuid = serviceId;
            _name = localName;

            _radio.StateChanged += (sender, ea) => {
                switch (_radio.State)
                {
                    case RadioState.Unknown:
                        break;
                    case RadioState.On:
                        if (_wasEnabled)
                            this.StartListener();
                        break;
                    case RadioState.Off:
                        this.CleanupListener();
                        break;
                    case RadioState.Disabled:
                        this.CleanupListener();
                        break;
                    default:
                        break;
                }
            };
        }

        void IBtSvcCmdHandler.HandleConnect(ConnectBluetoothCmd cmd)
        {
            try
            {
                if (_knownDevicesByAddress.TryGetValue(cmd.Address, out var deviceEntry))
                {
                    //var service = deviceEntry.Services.FirstOrDefault(s => s.ServiceId.Uuid == _serviceGuid);
                    //if (service != null)

                    var result = deviceEntry.Device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(_serviceGuid), BluetoothCacheMode.Uncached).GetAwaiter().GetResult();
                    if (result.Error == BluetoothError.Success && result.Services.Count > 0)
                    {
                        var service = result.Services.First();
                        var sck = new StreamSocket();
                        sck.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName).GetAwaiter().GetResult();

                        this.HandleConnection(cmd, sck);
                    }
                }
                else
                {
                    throw new ApplicationException("Desired device not found");
                }
            }
            catch (Exception ex)
            {
                cmd.OnErrorCallback?.Invoke(ex);
            }
        }

        void IBtSvcCmdHandler.HandleEnable(EnableBluetoothCmd cmd)
        {
            this.StartListener();
            _wasEnabled = true;
        }

        void IBtSvcCmdHandler.HandleDisable(DisableBluetoothCmd cmd)
        {
            this.CleanupListener();
            _watcher.Stop();
            _wasEnabled = false;
        }

        void IBtSvcCmdHandler.HandleRefresh(RefreshBluetoothCmd cmd)
        {
            // if (this.IsEnabled && !this.IsDiscovering && !BluetoothAdapter.DefaultAdapter.IsDiscovering)
            //if (_adapter.IsEnabled && !_adapter.IsDiscovering)
            //{
            //    _knownDevicesByAddress.Clear();

            //    var task = DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(PlayerService.Id)));

            //    _adapter.BondedDevices.ForEach(this.RegisterDevice);
            //    _adapter.StartDiscovery();
            //}

            // Request additional properties

            if (_radio.State == RadioState.On && _watcher == null)
            {
                string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

                var deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                                requestedProperties,
                                                                DeviceInformationKind.AssociationEndpoint);

                _watcher = deviceWatcher;

                // Hook up handlers for the watcher events before starting the watcher
                deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) => {
                    if (deviceInfo.Name != "")
                        this.RegisterDevice(deviceInfo);
                });

                deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) => {
                    //if (deviceInfoUpdate.Name != "")
                    //    this.RegisterDevice(deviceInfoUpdate);
                });

                deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) => {
                    //
                });

                deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) => {
                    //
                });

                deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) => {
                    deviceWatcher = null;
                });

                deviceWatcher.Start();
            }
        }

        private void RegisterDevice(DeviceInformation deviceInfo)
        {
            if (_knownDevicesByAddress.TryGetValue(deviceInfo.Id, out var knownDevice))
            {
                // rediscovered, update service presented flag?
            }
            else
            {
                var bluetoothDevice = BluetoothDevice.FromIdAsync(deviceInfo.Id).GetAwaiter().GetResult();
                //var result = bluetoothDevice.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(_serviceGuid), BluetoothCacheMode.Uncached).GetAwaiter().GetResult();
                //var hasService = result.Error == BluetoothError.Success && result.Services.Count > 0;
                //var entry = new BtDeviceEntry(bluetoothDevice, result.Services, new BtDeviceEntryInfo(bluetoothDevice.BluetoothAddress.ToString(), deviceInfo.Name, hasService));

                var hasService = false;
                var entry = new BtDeviceEntry(bluetoothDevice, new BtDeviceEntryInfo(deviceInfo.Id, deviceInfo.Name, hasService));
                _knownDevicesByAddress.Add(deviceInfo.Id, entry);
                this.OnDiscover?.Invoke(entry.Info);
            }
        }

        private void HandleConnection(ConnectBluetoothCmd cmd, StreamSocket socket)
        {
            var stream = new BluetoothStream(socket);
            
            _negotiator.NegotiateAsync(stream, _serviceGuid, _name, r => {
                if (r.Successed)
                {
                    var info = new BtDeviceEntryInfo(socket.Information.RemoteAddress.ToString(), r.PeerName, true);
                    var cnn = new BluetoothDeviceChannel(socket, r.Stream, r.AsyncWriter, info);
                    this.OnConnection?.Invoke(cnn);
                    cmd?.OnSuccessCallback?.Invoke(cnn);
                }
                else
                {
                    this.OnError?.Invoke(r.Exception);
                    cmd?.OnErrorCallback?.Invoke(r.Exception);
                }
            });
        }

        private void StartListener()
        {
            if (!this.IsEnabled)
            {
                if (_radio.State == RadioState.On)
                {
                    this.IsEnabled = true;
                    try
                    {
                        _listener = new BtListener(_serviceGuid);
                        _listener.OnConnection += sck => {
                            // Note - this is the supported way to get a Bluetooth device from a given socket
                            // var remoteDevice = await BluetoothDevice.FromHostNameAsync(sck.Information.RemoteHostName);
                            this.HandleConnection(null, sck);
                        };
                        // _listener.OnError += ex => this.CleanupListener();
                        _listener.Start();
                        this.OnStateChanged(this.IsEnabled);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Enabling network listener");
                        this.CleanupListener();
                    }
                }
                else
                {
                    this.OnStateChanged(this.IsEnabled);
                }
            }
        }

        private void CleanupListener()
        {
            if (_listener != null)
            {
                _listener.SafeDispose();
                _listener = null;
                this.IsEnabled = false;
                this.OnStateChanged(this.IsEnabled);
            }
        }

        protected override void Cleanup()
        {
            this.CleanupListener();
            //_service.UnregisterReceiver(_btDiscoveryIntentReceiver);
        }

        public void Connect(string address, Action<IBtDeviceChannel> onSuccess, Action<Exception> onError)
        {
            this.Post(new ConnectBluetoothCmd(address, onSuccess, onError));
        }

        public void Enable()
        {
            this.Post(new EnableBluetoothCmd());
        }

        public void Disable()
        {
            this.Post(new DisableBluetoothCmd());
        }

        public void Refresh()
        {
            this.Post(new RefreshBluetoothCmd());
        }
    }
}
