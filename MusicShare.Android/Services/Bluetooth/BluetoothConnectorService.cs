using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Streams;
using Uuid = Java.Util.UUID;

namespace MusicShare.Droid.Services.Bluetooth
{

    internal class BluetoothConnectorService : ServiceWorker<BluetoothServieCommand, IBtSvcCmdHandler>, IBtSvcCmdHandler, IBluetoothConnector, IBluetoothConnectorImpl
    {
        private class BtDiscoveryIntentReceiver : BroadcastReceiver
        {
            public event Action<BluetoothDevice> OnDeviceDiscover;
            public event Action OnDiscoveryFinished;
            public event Action OnBtActivated;
            public event Action OnBtDeactivated;

            public BtDiscoveryIntentReceiver()
            {
            }

            public override void OnReceive(Context context, Intent intent)
            {
                switch (intent.Action)
                {
                    case BluetoothDevice.ActionFound:
                        {
                            var extra = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                            if (extra is BluetoothDevice device)
                            {
                                this.OnDeviceDiscover?.Invoke(device);
                            }
                        }
                        break;
                    case BluetoothAdapter.ActionDiscoveryFinished:
                        {
                            this.OnDiscoveryFinished?.Invoke();
                        }
                        break;
                    case BluetoothAdapter.ActionStateChanged:
                        {
                            // var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                            // Toast.MakeText(context, "BluetoothDevice:" + device.Name + "Changed", ToastLength.Short).Show();
                            var blueState = (State)intent.GetIntExtra(BluetoothAdapter.ExtraState, 0);
                            switch (blueState)
                            {
                                case State.On:
                                    this.OnBtActivated?.Invoke();
                                    break;
                                case State.TurningOff:
                                case State.Off:
                                    this.OnBtDeactivated?.Invoke();
                                    break;
                            }
                        }
                        break;
                    default: // do nothing
                        break;
                }
            }
        }

        private class BtListener : DisposableObject
        {
            public event Action<BluetoothSocket> OnConnection;
            // public event Action<Exception> OnError;

            private readonly BluetoothAdapter _adapter;
            private readonly BluetoothServerSocket _listener;
            private readonly Thread _acceptThread;

            public BtListener(BluetoothAdapter adapter, Uuid serviceId)
            {
                _adapter = adapter;
                _listener = adapter.ListenUsingRfcommWithServiceRecord(PlayerService.Name, serviceId);

                _acceptThread = new Thread(this.AcceptingThreadProc) {
                    IsBackground = true
                };
            }

            public void Start()
            {
                _acceptThread.Start();
            }

            private void AcceptingThreadProc()
            {
                while (!this.IsDisposed && _adapter.IsEnabled)
                {
                    BluetoothSocket sck;
                    try { sck = _listener.Accept(); }
                    catch (Exception) { sck = null; }

                    if (sck != null)
                    {
                        this.OnConnection?.Invoke(sck);
                    }
                }
            }

            protected override void DisposeImpl()
            {
                try { _listener.Close(); }
                catch (Exception ex)
                {
                    Log.Error(ex, "Stopping BtListener");
                }

                _listener.SafeDispose();
            }
        }

        private class BtDeviceEntry
        {
            public BluetoothDevice Device { get; }
            public BtDeviceEntryInfo Info { get; }

            public BtDeviceEntry(BluetoothDevice device, BtDeviceEntryInfo info)
            {
                this.Device = device;
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
        private readonly Uuid _serviceUuid;
        private readonly string _name;
        private readonly Dictionary<string, BtDeviceEntry> _knownDevicesByAddress = new Dictionary<string, BtDeviceEntry>(StringComparer.InvariantCultureIgnoreCase);
        private readonly BtDiscoveryIntentReceiver _btDiscoveryIntentReceiver;
        private readonly Service _service;
        private readonly BluetoothAdapter _adapter;

        private BtListener _listener = null;

        private bool _wasEnabled = false;
        public bool IsEnabled { get; private set; } = false;

        public BluetoothConnectorService(StreamNegotiator negotiator, Guid serviceId, string localName, Service service)
        {
            _negotiator = negotiator;
            _serviceGuid = serviceId;
            _serviceUuid = Uuid.FromString(serviceId.ToString());
            _name = localName;

            _service = service;
            _adapter = BluetoothAdapter.DefaultAdapter;

            _btDiscoveryIntentReceiver = new BtDiscoveryIntentReceiver();
            _btDiscoveryIntentReceiver.OnDeviceDiscover += device => {
                this.RegisterDevice(device);
            };
            _btDiscoveryIntentReceiver.OnDiscoveryFinished += () => {

            };
            _btDiscoveryIntentReceiver.OnBtActivated += () => {
                if (_wasEnabled)
                    this.StartListener();
            };
            _btDiscoveryIntentReceiver.OnBtDeactivated += () => {
                this.CleanupListener();
            };

            IntentFilter filter = new IntentFilter();
            filter.AddAction(BluetoothDevice.ActionFound);
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
            filter.AddAction(BluetoothAdapter.ActionStateChanged);
            _service.RegisterReceiver(_btDiscoveryIntentReceiver, filter);
        }

        void IBtSvcCmdHandler.HandleConnect(ConnectBluetoothCmd cmd)
        {
            try
            {
                if (_knownDevicesByAddress.TryGetValue(cmd.Address, out var deviceEntry))
                {
                    var sck = deviceEntry.Device.CreateRfcommSocketToServiceRecord(_serviceUuid);
                    sck.Connect();

                    this.HandleConnection(cmd, sck);
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
            _wasEnabled = false;
        }

        void IBtSvcCmdHandler.HandleRefresh(RefreshBluetoothCmd cmd)
        {
            // if (this.IsEnabled && !this.IsDiscovering && !BluetoothAdapter.DefaultAdapter.IsDiscovering)
            if (_adapter.IsEnabled && !_adapter.IsDiscovering)
            {
                _knownDevicesByAddress.Clear();

                _adapter.BondedDevices.ForEach(this.RegisterDevice);
                _adapter.StartDiscovery();
            }
        }

        private void RegisterDevice(BluetoothDevice device)
        {
            if (_knownDevicesByAddress.TryGetValue(device.Address, out var knownDevice))
            {
                // rediscovered, update service presented flag?
            }
            else
            {
                // device.FetchUuidsWithSdp() // see https://stackoverflow.com/questions/14812326/android-bluetooth-get-uuids-of-discovered-devices
                var hasService = device.GetUuids()?.Any(uuid => uuid.Uuid.Equals(_serviceUuid)) ?? false;
                var entry = new BtDeviceEntry(device, new BtDeviceEntryInfo(device.Address, device.Name, hasService));
                _knownDevicesByAddress.Add(device.Address, entry);
                this.OnDiscover?.Invoke(entry.Info);
            }
        }

        private void HandleConnection(ConnectBluetoothCmd cmd, BluetoothSocket socket)
        {
            var stream = new BidirectionalStream(socket.InputStream, socket.OutputStream);

            _negotiator.NegotiateAsync(stream, _serviceGuid, _name, r => {
                if (r.Successed)
                {
                    var cnn = new BluetoothDeviceChannel(socket, stream, r.AsyncWriter, r.PeerName);
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
                if (_adapter.IsEnabled)
                {
                    this.IsEnabled = true;
                    try
                    {
                        _listener = new BtListener(_adapter, _serviceUuid);
                        _listener.OnConnection += sck => this.HandleConnection(null, sck);
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
            _service.UnregisterReceiver(_btDiscoveryIntentReceiver);
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
