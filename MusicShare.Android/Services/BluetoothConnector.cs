using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Interaction.Standard.Common;
using MusicShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MusicShare.Droid.Services
{
    class BtDeviceConnection : DisposableObject
    {
        public BluetoothSocket Socket { get; }
        public BidirectionalStream Stream { get; }

        public BtDeviceConnection(BluetoothSocket socket)
        {
            this.Socket = socket;
            this.Stream = new BidirectionalStream(socket.InputStream, socket.OutputStream);
        }

        protected override void DisposeImpl()
        {
            this.Stream.SafeDispose();
            this.Socket.SafeDispose();
        }
    }

    class BluetoothConnector : DisposableObject, IBluetoothConnector
    {
        class BtDiscoveryIntentReceiver : BroadcastReceiver
        {
            readonly BluetoothConnector _owner;

            public BtDiscoveryIntentReceiver(BluetoothConnector owner)
            {
                _owner = owner;
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
                                _owner.OnDeviceDiscovered(device);
                            }
                        }
                        break;
                    case BluetoothAdapter.ActionDiscoveryFinished:
                        {
                            _owner.OnDiscoveryFinished();
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
                                    _owner.OnBtActivated();
                                    break;
                                case State.TurningOff:
                                case State.Off:
                                    _owner.OnBtDeactivated();
                                    break;
                            }
                        }
                        break;
                    default: // do nothing
                        break;
                }
            }
        }

        class BtDeviceEntry
        {
            public BluetoothDevice Device { get; }
            public BtDeviceEntryInfo Info { get; }

            public BtDeviceEntry(BluetoothDevice device, BtDeviceEntryInfo info)
            {
                this.Device = device;
                this.Info = info;
            }
        }

        public event Action OnActivated;
        public event Action OnDeactivate;

        public event Action OnDiscoverReset;
        public event Action<BtDeviceEntryInfo> OnDiscoverFound;

        public event Action<BtDeviceConnection> OnNewConnection;

        static readonly Java.Util.UUID _btSericeId = Java.Util.UUID.FromString("EDD0608E-BA9B-497B-82A7-A2F79D5FC346");

        readonly Service _service;
        readonly BtDiscoveryIntentReceiver _btDiscoveryIntentReceiver;

        readonly Dictionary<string, BtDeviceEntry> _knownDevices = new Dictionary<string, BtDeviceEntry>();

        public bool IsEnabled { get; private set; } = false;
        public bool IsDiscovering { get; private set; } = false;

        readonly ManualResetEvent _disposingEv = new ManualResetEvent(false);
        readonly ManualResetEvent _acceptingThreadEv = new ManualResetEvent(false);
        readonly Thread _acceptingThread;

        public BluetoothConnector(Service service)
        {
            _service = service;
            _btDiscoveryIntentReceiver = new BtDiscoveryIntentReceiver(this);

            IntentFilter filter = new IntentFilter();
            filter.AddAction(BluetoothDevice.ActionFound);
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
            filter.AddAction(BluetoothAdapter.ActionStateChanged);
            _service.RegisterReceiver(_btDiscoveryIntentReceiver, filter);

            if (BluetoothAdapter.DefaultAdapter.IsEnabled)
                this.OnBtActivated();

            _acceptingThread = new Thread(this.AcceptingThreadProc);
            _acceptingThread.Start();
        }

        protected override void DisposeImpl()
        {
            _disposingEv.Set();
            _service.UnregisterReceiver(_btDiscoveryIntentReceiver);
        }

        private void OnBtActivated()
        {
            this.IsEnabled = true;
            this.OnActivated?.Invoke();
            _acceptingThreadEv.Set();
        }

        private void OnBtDeactivated()
        {
            this.IsEnabled = false;
            _acceptingThreadEv.Reset();
            this.OnDeactivate?.Invoke();
        }

        #region discoverability

        public void RefreshDevices()
        {
            if (this.IsEnabled && !this.IsDiscovering && !BluetoothAdapter.DefaultAdapter.IsDiscovering)
            {
                _knownDevices.Clear();
                this.OnDiscoverReset?.Invoke();

                BluetoothAdapter.DefaultAdapter.BondedDevices.ForEach(this.OnDeviceDiscovered);

                this.IsDiscovering = true;
                BluetoothAdapter.DefaultAdapter.StartDiscovery();
            }
        }

        private void OnDeviceDiscovered(BluetoothDevice device)
        {
            var entry = new BtDeviceEntry(device, new BtDeviceEntryInfo(device.Address, device.Name));
            _knownDevices[device.Address] = entry;
            this.OnDiscoverFound?.Invoke(entry.Info);
        }

        private void OnDiscoveryFinished()
        {
            if (this.IsDiscovering)
            {
                this.IsDiscovering = false;
            }
        }

        #endregion

        private void AcceptingThreadProc()
        {
            using (var listener = BluetoothAdapter.DefaultAdapter.ListenUsingRfcommWithServiceRecord(PlayerService.Name, _btSericeId))
            {
                while (WaitHandle.WaitAny(new[] { _acceptingThreadEv, _disposingEv }) == 0)
                {
                    BluetoothSocket sck;
                    try { sck = listener.Accept(); }
                    catch (Exception) { sck = null; }

                    if (sck != null)
                    {
                        this.OnNewConnection?.Invoke(new BtDeviceConnection(sck));
                    }
                }
            }
        }

        public void ConnectBt(string addr)
        {
            if (_knownDevices.TryGetValue(addr, out var deviceEntry))
            {
                var sck = deviceEntry.Device.CreateRfcommSocketToServiceRecord(_btSericeId);
                sck.Connect();
                this.OnNewConnection?.Invoke(new BtDeviceConnection(sck));
            }
        }
    }
}