using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using Xamarin.Forms;
using ZXing;

namespace MusicShare.ViewModels.Home
{
    public abstract class DeviceEntry : BindableObject
    {
        #region bool IsConnectBtnVisible 

        public bool IsConnectBtnVisible
        {
            get { return (bool)this.GetValue(IsConnectBtnVisibleProperty); }
            set { this.SetValue(IsConnectBtnVisibleProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsConnectBtnVisible . This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsConnectBtnVisibleProperty =
            BindableProperty.Create("IsConnectBtnVisible", typeof(bool), typeof(DeviceEntry), default(bool));

        #endregion

        #region bool IsDisconnectBtnVisible 

        public bool IsDisconnectBtnVisible
        {
            get { return (bool)this.GetValue(IsDisconnectBtnVisibleProperty); }
            set { this.SetValue(IsDisconnectBtnVisibleProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsDisconnectBtnVisible.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsDisconnectBtnVisibleProperty =
            BindableProperty.Create("IsDisconnectBtnVisible", typeof(bool), typeof(DeviceEntry), default(bool));

        #endregion

        public ConnectivityViewModel Owner { get; }

        public abstract string Title { get; }
        public abstract string Ping { get; }
        public abstract bool IsConnected { get; }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        protected DeviceEntry(ConnectivityViewModel owner)
        {
            this.Owner = owner;

            this.ConnectCommand = new Command(async () => this.DoConnect());
            this.DisconnectCommand = new Command(async () => this.DoDisconnect());
        }

        protected abstract void DoConnect();
        protected abstract void DoDisconnect();

        //public void ForceBindingds()
        //{
        //    this.OnPropertyChanged("Title");
        //    this.OnPropertyChanged("Ping");
        //    this.OnPropertyChanged("IsConnected");
        //    this.OnPropertyChanged("IsConnectBtnVisible");
        //    this.OnPropertyChanged("ConnectCommand");
        //}

        public abstract override string ToString();
    }

    public class BtDeviceEntry : DeviceEntry
    {
        public BtDeviceEntryInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        // public override string Ping { get { return "[" + this.Info.Address + "]"; } }
        public override string Ping { get { return "𝄐"; } }
        public override bool IsConnected { get { return false; } }

        public BtDeviceEntry(ConnectivityViewModel owner, BtDeviceEntryInfo info)
            : base(owner)
        {
            this.Info = info;
        }

        protected override async void DoConnect()
        {
            this.Owner.DoBtConnect(this);
        }

        protected override void DoDisconnect()
        {
            // do nothing
        }

        public override string ToString()
        {
            return "BT|" + this.Info.Address;
        }
    }

    public class NetDeviceEntry : DeviceEntry
    {
        public NetHostInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        // public override string Ping { get { return this.Info.Ping.Truncate(TimeSpan.FromMilliseconds(1)).TotalSeconds + "[" + this.Info.Address + "]"; } }
        public override string Ping { get { return this.Info.Ping.Truncate(TimeSpan.FromMilliseconds(1)).TotalSeconds.ToString("0.###"); } }
        public override bool IsConnected { get { return false; } }

        public NetDeviceEntry(ConnectivityViewModel owner, NetHostInfo info)
            : base(owner)
        {
            this.Info = info;
        }

        protected override async void DoConnect()
        {
            this.Owner.DoNetConnect(this);
        }

        protected override void DoDisconnect()
        {
            // do nothing
        }

        public override string ToString()
        {
            return "NET|" + this.Info.Address;
        }
    }

    public class DeviceChannelEntry : DeviceEntry
    {
        private DeviceEntry _entry;

        public override string Title { get { return _entry.Title; } }
        public override string Ping { get { return _entry.Ping; } }
        public override bool IsConnected { get { return true; } }

        public IDeviceChannel Channel { get; }

        public DeviceChannelEntry(ConnectivityViewModel owner, IDeviceChannel chan)
            : base(owner)
        {
            this.Channel = chan;

            switch (chan)
            {
                case IBtDeviceChannel btChan:
                    _entry = new BtDeviceEntry(owner, btChan.Info);
                    break;
                case INetDeviceChannel netChan:
                    _entry = new NetDeviceEntry(owner, netChan.Info);
                    break;
                default:
                    throw new NotImplementedException("unknown channel kind");
            }
        }

        protected override void DoConnect()
        {
            // do nothing
        }

        protected override void DoDisconnect()
        {
            this.Owner.DoDisconnect(this);
        }


        public override string ToString()
        {
            return "CHAN|" + _entry.ToString();
        }
    }

    public enum ConnectivityMode
    {
        Bluetooth,
        Lan,
        Wan
    }

    public class ConnectivityViewModel : MenuPageViewModel
    {
        // bool _wasWanActivated = true, _wasLanActivated = true, _wasBtActivated = true;

        #region bool IsSharingActivated 

        public bool IsSharingActivated
        {
            get { return (bool)this.GetValue(IsSharingActivatedProperty); }
            set { this.SetValue(IsSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsSharingActivatedProperty =
            BindableProperty.Create("IsSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool), propertyChanging: IsSharingActivatedChanging);

        #endregion

        private static void IsSharingActivatedChanging(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is ConnectivityViewModel cvm && newValue is bool activate)
            //cvm.SwitchConnectivitySharing(false,null,activate);
        }

        #region bool IsWanSharingEnabled 

        public bool IsWanSharingEnabled
        {
            get { return (bool)this.GetValue(IsWanSharingEnabledProperty); }
            set { this.SetValue(IsWanSharingEnabledProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsWanSharingEnabled.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsWanSharingEnabledProperty =
            BindableProperty.Create("IsWanSharingEnabled", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsWanSharingActivated 

        public bool IsWanSharingActivated
        {
            get { return (bool)this.GetValue(IsWanSharingActivatedProperty); }
            set { this.SetValue(IsWanSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsWanSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsWanSharingActivatedProperty =
            BindableProperty.Create("IsWanSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool), propertyChanging: IsWanSharingActivatedChanging);

        #endregion

        private static void IsWanSharingActivatedChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ConnectivityViewModel cvm && newValue is bool activate)
                cvm.SwitchConnectivitySharing(false, ConnectivityMode.Wan, activate);
        }

        #region bool IsLanSharingEnabled 

        public bool IsLanSharingEnabled
        {
            get { return (bool)this.GetValue(IsLanSharingEnabledProperty); }
            set { this.SetValue(IsLanSharingEnabledProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsLanSharingEnabled.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsLanSharingEnabledProperty =
            BindableProperty.Create("IsLanSharingEnabled", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsLanSharingActivated 

        public bool IsLanSharingActivated
        {
            get { return (bool)this.GetValue(IsLanSharingActivatedProperty); }
            set { this.SetValue(IsLanSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsLanSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsLanSharingActivatedProperty =
            BindableProperty.Create("IsLanSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool), propertyChanging: IsLanSharingActivatedChanging);

        #endregion

        private static void IsLanSharingActivatedChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ConnectivityViewModel cvm && newValue is bool activate)
                cvm.SwitchConnectivitySharing(false, ConnectivityMode.Lan, activate);
        }

        #region bool IsBtSharingEnabled

        public bool IsBtSharingEnabled
        {
            get { return (bool)this.GetValue(IsBtSharingEnabledProperty); }
            set { this.SetValue(IsBtSharingEnabledProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsBtSharingEnabled.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsBtSharingEnabledProperty =
            BindableProperty.Create("IsBtSharingEnabled", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsBtSharingActivated 

        public bool IsBtSharingActivated
        {
            get { return (bool)this.GetValue(IsBtSharingActivatedProperty); }
            set { this.SetValue(IsBtSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsBtSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsBtSharingActivatedProperty =
            BindableProperty.Create("IsBtSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool), propertyChanging: IsBtSharingActivatedChanging);

        #endregion

        private static void IsBtSharingActivatedChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ConnectivityViewModel cvm && newValue is bool activate)
                cvm.SwitchConnectivitySharing(false, ConnectivityMode.Bluetooth, activate);
        }

        #region bool PickUpActivated 

        public bool PickUpActivated
        {
            get { return (bool)this.GetValue(PickUpActivatedProperty); }
            set { this.SetValue(PickUpActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for PickUpActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty PickUpActivatedProperty =
            BindableProperty.Create("PickUpActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool), propertyChanging: OnPickUpActivatedChanging);

        #endregion

        private static void OnPickUpActivatedChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ConnectivityViewModel cvm && newValue is bool activate && activate)
                cvm.RefreshCommand.Execute(null);
        }

        #region ObservableCollection<DeviceEntry> Devices 

        public ObservableCollection<DeviceEntry> Devices
        {
            get { return (ObservableCollection<DeviceEntry>)this.GetValue(DevicesProperty); }
            set { this.SetValue(DevicesProperty, value); }
        }

        // Using a BindableProperty as the backing store for Devices. This enables animation, styling, binding, etc...
        public static readonly BindableProperty DevicesProperty =
            BindableProperty.Create("Devices", typeof(ObservableCollection<DeviceEntry>), typeof(ConnectivityViewModel), default(ObservableCollection<DeviceEntry>));

        #endregion

        #region DeviceEntry SelectedDevice 

        public DeviceEntry SelectedDevice
        {
            get { return (DeviceEntry)this.GetValue(SelectedDeviceProperty); }
            set { this.SetValue(SelectedDeviceProperty, value); }
        }

        // Using a BindableProperty as the backing store for SelectedDevice. This enables animation, styling, binding, etc...
        public static readonly BindableProperty SelectedDeviceProperty =
            BindableProperty.Create("SelectedDevice", typeof(DeviceEntry), typeof(ConnectivityViewModel), default(DeviceEntry),
                                     propertyChanging: OnSelectedDeviceChanging);

        #endregion

        private static void OnSelectedDeviceChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (oldValue is DeviceEntry deselected)
            {
                deselected.IsDisconnectBtnVisible = false;
                deselected.IsConnectBtnVisible = false;
            }

            if (newValue is DeviceEntry selected)
            {
                selected.IsConnectBtnVisible = !selected.IsConnected;
                selected.IsDisconnectBtnVisible = selected.IsConnected;
            }
        }

        public ICommand MakeBeaconCommand { get; }
        public ICommand MakeSpotCommand { get; }
        public ICommand CloseQrCommand { get; }

        #region bool IsQrVisible 

        public bool IsQrVisible
        {
            get { return (bool)this.GetValue(IsQrVisibleProperty); }
            set { this.SetValue(IsQrVisibleProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsQrVisible. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsQrVisibleProperty =
            BindableProperty.Create("IsQrVisible", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region object QrContent 

        public object QrContent
        {
            get { return this.GetValue(QrContentProperty); }
            set { this.SetValue(QrContentProperty, value); }
        }

        // Using a BindableProperty as the backing store for QrContent. This enables animation, styling, binding, etc...
        public static readonly BindableProperty QrContentProperty =
            BindableProperty.Create("QrContent", typeof(object), typeof(ConnectivityViewModel), default(object));

        #endregion

        private readonly Dictionary<string, DeviceEntry> _knownDevices = new Dictionary<string, DeviceEntry>();
        private readonly List<DeviceEntry> _channels = new List<DeviceEntry>();
        private readonly AppViewModel _app;

        public ConnectivityViewModel(AppStateGroupViewModel group)
            : base("Connectivity", group)
        {
            var app = _app = this.App;

            this.IsSharingActivated = false;
            this.IsBtSharingActivated = false;
            this.IsLanSharingActivated = false;
            this.IsWanSharingActivated = false;
            this.IsBtSharingEnabled = true;
            this.IsLanSharingEnabled = true;
            this.IsWanSharingEnabled = true;

            this.Devices = new ObservableCollection<DeviceEntry>();

            var player = ServiceContext.Instance.Player;

            player.OnConnection += chan => this.InvokeAction(() => {
                var newEntry = new DeviceChannelEntry(this, chan);
                chan.OnClosed += () => this.InvokeAction(() => {
                    _channels.Remove(newEntry);
                    this.Devices.Remove(newEntry);
                });

                _channels.Add(newEntry);
                this.Devices.Insert(0, newEntry);
            });

            player.BtConnector.OnStateChanged += ok => this.InvokeAction(() => {
                this.SwitchConnectivitySharing(true, ConnectivityMode.Bluetooth, ok);
            });
            player.BtConnector.OnDiscover += (d) => this.InvokeAction(() => {
                this.RegisterDiscoveredDevice(new BtDeviceEntry(this, d));
            });

            player.NetConnector.OnStateChanged += ok => this.InvokeAction(() => {
                this.SwitchConnectivitySharing(true, ConnectivityMode.Lan, ok);
            });
            player.NetConnector.OnDiscover += (d) => this.InvokeAction(() => {
                this.RegisterDiscoveredDevice(new NetDeviceEntry(this, d));
            });

            this.MakeBeaconCommand = new Command(async () => {
                var info = player.GetConnectivityInfo();
                var xs = new XmlSerializer(info.GetType());
                var ms = new StringWriter();
                xs.Serialize(ms, info);
                ms.Flush();
                this.GenerateQr(ms.ToString());
            });
            this.MakeSpotCommand = new Command(async () => {
                var connectivityInfo = await this.ScanQr();
                if (connectivityInfo != null)
                {
                    app.OperationInProgress = true;
                    player.Connect(connectivityInfo, ok => this.InvokeAction(() => {
                        app.OperationInProgress = false;

                        if (!ok)
                            app.PostPopup(PopupEntrySeverity.Warning, $"Failed to connect to {connectivityInfo.DeviceName} by beacon");
                    }));
                }
            });
            this.CloseQrCommand = new Command(async () => {
                this.IsQrVisible = false;
                this.QrContent = null;
            });
            this.IsRefreshAvailable = true;
        }

        private void RegisterDiscoveredDevice(DeviceEntry entry)
        {
            if (_knownDevices.TryGetValue(entry.ToString(), out var oldEntry))
                this.Devices.Remove(oldEntry);

            _knownDevices[entry.ToString()] = entry;
            this.Devices.Add(entry);
        }

        public async override void OnRefresh()
        {
            this.IsRefreshing = true;

            this.Devices.Clear();
            _knownDevices.Clear();
            _channels.ForEach(c => this.Devices.Insert(0, c));

            var player = ServiceContext.Instance.Player;
            player.BtConnector.Refresh();
            player.NetConnector.Refresh();

            await Task.Delay(5000);
            this.IsRefreshing = false;
        }

        private void GenerateQr(string codeValue)
        {
            var qrCode = new ZXing.Net.Mobile.Forms.ZXingBarcodeImageView {
                BarcodeFormat = BarcodeFormat.QR_CODE,
                BarcodeOptions = new ZXing.Common.EncodingOptions() {
                    Height = 350,
                    Width = 350,
                },
                BarcodeValue = codeValue,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
            };
            // Workaround for iOS
            qrCode.WidthRequest = 350;
            qrCode.HeightRequest = 350;

            this.QrContent = qrCode;
            this.IsQrVisible = true;
        }

        private async Task<ConnectivityInfoType> ScanQr()
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            try
            {
                var result = await scanner.Scan();
                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                {
                    var xs = new XmlSerializer(typeof(ConnectivityInfoType));
                    var info = xs.Deserialize(new StringReader(result.Text)) as ConnectivityInfoType;
                    return info;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Failed to scan QR: " + ex.ToString());
                return null;
            }
        }

        //class DemoDeviceEntry : DeviceEntry
        //{
        //    private DeviceEntry _entry;

        //    public override string Title { get { return _entry.Title; } }
        //    public override string Ping { get { return _entry.Ping; } }
        //    public override bool IsConnected { get { return true; } }

        //    public DemoDeviceEntry(ConnectivityViewModel owner, DeviceEntry entry)
        //        : base(owner)
        //    {
        //        _entry = entry;
        //    }

        //    protected override async void DoConnect()
        //    {
        //        // do nothing
        //    }
        //}

        //private async void EmulateConnect(DeviceEntry entry)
        //{
        //    _app.OperationInProgress = true;

        //    await Task.Delay(2000);
        //    var newEntry = new DemoDeviceEntry(this, entry);
        //    _channels.Add(newEntry);
        //    this.Devices.Insert(0, newEntry);

        //    _app.OperationInProgress = false;
        //}

        private bool _switching = false;

        private void SwitchConnectivitySharing(bool feedback, ConnectivityMode mode, bool turnOn)
        {
            if (feedback)
            {
                switch (mode)
                {
                    case ConnectivityMode.Bluetooth:
                        this.IsBtSharingActivated = turnOn;
                        this.IsBtSharingEnabled = true;
                        break;
                    case ConnectivityMode.Lan:
                        this.IsLanSharingActivated = turnOn;
                        this.IsLanSharingEnabled = true;
                        break;
                    case ConnectivityMode.Wan:
                        break;
                    default:
                        break;
                }
            }
            else if (!_switching)
            {
                _switching = true;
                switch (mode)
                {
                    case ConnectivityMode.Bluetooth:
                        {
                            this.IsBtSharingEnabled = false;

                            if (turnOn)
                                ServiceContext.Instance.Player.BtConnector.Enable();
                            else
                                ServiceContext.Instance.Player.BtConnector.Disable();
                        }
                        break;
                    case ConnectivityMode.Lan:
                        {
                            this.IsLanSharingEnabled = false;

                            if (turnOn)
                                ServiceContext.Instance.Player.NetConnector.Enable();
                            else
                                ServiceContext.Instance.Player.NetConnector.Disable();
                        }
                        break;
                    case ConnectivityMode.Wan:
                        break;
                    default:
                        break;
                }
                _switching = false;
            }

        }

        internal void DoBtConnect(BtDeviceEntry btDeviceEntry)
        {
            _app.OperationInProgress = true;

            ServiceContext.Instance.Player.BtConnector.Connect(btDeviceEntry.Info.Address, cnn => this.InvokeAction(() => {
                _app.OperationInProgress = false;
            }), ex => this.InvokeAction(() => {
                _app.OperationInProgress = false;
                _app.PostPopup(PopupEntrySeverity.Warning, $"Failed to connect to {btDeviceEntry.Info.Name} by bluetooth");
            }));
        }

        internal void DoNetConnect(NetDeviceEntry netDeviceEntry)
        {
            var info = netDeviceEntry.Info;
            _app.OperationInProgress = true;

            ServiceContext.Instance.Player.NetConnector.Connect(info.Address, info.Port, cnn => this.InvokeAction(() => {
                _app.OperationInProgress = false;
            }), ex => this.InvokeAction(() => {
                _app.OperationInProgress = false;
                _app.PostPopup(PopupEntrySeverity.Warning, $"Failed to connect to {netDeviceEntry.Info.Name} by network");
            }));
        }

        internal void DoDisconnect(DeviceChannelEntry chanEntry)
        {
            _channels.Remove(chanEntry);
            this.Devices.Remove(chanEntry);
            chanEntry.Channel.SafeDispose();
        }
    }

}
