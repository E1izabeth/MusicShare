using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using Xamarin.Forms;
using ZXing;

namespace MusicShare.ViewModels.Home
{
    abstract class DeviceEntry : BindableObject
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

        public ConnectivityViewModel Owner { get; }

        public abstract string Title { get; }
        public abstract string Ping { get; }
        public abstract bool IsConnected { get; }

        public ICommand ConnectCommand { get; }

        protected DeviceEntry(ConnectivityViewModel owner)
        {
            this.Owner = owner;

            this.ConnectCommand = new Command(async () => this.DoConnect());
        }

        protected abstract void DoConnect();

        //public void ForceBindingds()
        //{
        //    this.OnPropertyChanged("Title");
        //    this.OnPropertyChanged("Ping");
        //    this.OnPropertyChanged("IsConnected");
        //    this.OnPropertyChanged("IsConnectBtnVisible");
        //    this.OnPropertyChanged("ConnectCommand");
        //}
    }

    class BtDeviceEntry : DeviceEntry
    {
        public BtDeviceEntryInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        public override string Ping { get { return "[" + this.Info.Address + "]"; } }
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
    }

    class NetDeviceEntry : DeviceEntry
    {
        public NetHostInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        public override string Ping { get { return this.Info.Ping.Truncate(TimeSpan.FromMilliseconds(1)).TotalSeconds + "[" + this.Info.Address + "]"; } }
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
    }

    class DeviceChannelEntry : DeviceEntry
    {
        private DeviceEntry _entry;

        public override string Title { get { return _entry.Title; } }
        public override string Ping { get { return _entry.Ping; } }
        public override bool IsConnected { get { return true; } }

        IDeviceChannel _chan;

        public DeviceChannelEntry(ConnectivityViewModel owner, IDeviceChannel chan)
            : base(owner)
        {
            _chan = chan;

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
    }

    class ConnectivityViewModel : MenuPageViewModel
    {
        #region bool IsSharingActivated 

        public bool IsSharingActivated
        {
            get { return (bool)this.GetValue(IsSharingActivatedProperty); }
            set { this.SetValue(IsSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsSharingActivatedProperty =
            BindableProperty.Create("IsSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsWanSharingActivated 

        public bool IsWanSharingActivated
        {
            get { return (bool)this.GetValue(IsWanSharingActivatedProperty); }
            set { this.SetValue(IsWanSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsWanSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsWanSharingActivatedProperty =
            BindableProperty.Create("IsWanSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsLanSharingActivated 

        public bool IsLanSharingActivated
        {
            get { return (bool)this.GetValue(IsLanSharingActivatedProperty); }
            set { this.SetValue(IsLanSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsLanSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsLanSharingActivatedProperty =
            BindableProperty.Create("IsLanSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool IsBtSharingActivated 

        public bool IsBtSharingActivated
        {
            get { return (bool)this.GetValue(IsBtSharingActivatedProperty); }
            set { this.SetValue(IsBtSharingActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsBtSharingActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsBtSharingActivatedProperty =
            BindableProperty.Create("IsBtSharingActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        #region bool PickUpActivated 

        public bool PickUpActivated
        {
            get { return (bool)this.GetValue(PickUpActivatedProperty); }
            set { this.SetValue(PickUpActivatedProperty, value); }
        }

        // Using a BindableProperty as the backing store for PickUpActivated. This enables animation, styling, binding, etc...
        public static readonly BindableProperty PickUpActivatedProperty =
            BindableProperty.Create("PickUpActivated", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

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

        static void OnSelectedDeviceChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (oldValue is DeviceEntry deselected && !deselected.IsConnected)
                deselected.IsConnectBtnVisible = false;

            if (newValue is DeviceEntry selected && !selected.IsConnected)
                selected.IsConnectBtnVisible = true;
        }

        #region bool IsRefreshingDevices 

        public bool IsRefreshingDevices
        {
            get { return (bool)this.GetValue(IsRefreshingDevicesProperty); }
            set { this.SetValue(IsRefreshingDevicesProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsRefreshingDevices. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsRefreshingDevicesProperty =
            BindableProperty.Create("IsRefreshingDevices", typeof(bool), typeof(ConnectivityViewModel), default(bool));

        #endregion

        public ICommand MakeBeaconCommand { get; }
        public ICommand MakeSpotCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
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
            get { return (object)this.GetValue(QrContentProperty); }
            set { this.SetValue(QrContentProperty, value); }
        }

        // Using a BindableProperty as the backing store for QrContent. This enables animation, styling, binding, etc...
        public static readonly BindableProperty QrContentProperty =
            BindableProperty.Create("QrContent", typeof(object), typeof(ConnectivityViewModel), default(object));

        #endregion


        readonly List<DeviceEntry> _channels = new List<DeviceEntry>();
        readonly AppViewModel _app;

        public ConnectivityViewModel(AppViewModel app)
            : base("Connectivity")
        {
            _app = app;
            this.IsSharingActivated = true;
            this.IsBtSharingActivated = true;
            this.IsLanSharingActivated = true;
            this.IsWanSharingActivated = true;

            this.Devices = new ObservableCollection<DeviceEntry>();

            var player = ServiceContext.Instance.Player;

            player.OnConnection += chan => this.InvokeAction(() => {
                var newEntry = new DeviceChannelEntry(this, chan);
                _channels.Add(newEntry);
                this.Devices.Insert(0, newEntry);
            });

            player.BtConnector.OnActivated += () => this.InvokeAction(() => {
                this.IsBtSharingActivated = true;
            });
            player.BtConnector.OnDeactivate += () => this.InvokeAction(() => {
                this.IsBtSharingActivated = false;
            });
            player.BtConnector.OnDiscoverFound += (d) => this.InvokeAction(() => {
                this.Devices.Add(new BtDeviceEntry(this, d));
            });

            player.NetConnector.OnDiscoverFound += (d) => this.InvokeAction(() => {
                this.Devices.Add(new NetDeviceEntry(this, d));
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
                    player.Connect(connectivityInfo, () => this.InvokeAction(() => {
                        app.OperationInProgress = false;
                    }));
                }
            });
            this.RefreshDevicesCommand = new Command(async () => {
                this.IsRefreshingDevices = true;

                this.Devices.Clear();
                _channels.ForEach(c => this.Devices.Insert(0, c));

                player.BtConnector.RefreshDevices();
                player.NetConnector.RefreshHosts();

                await Task.Delay(5000);
                this.IsRefreshingDevices = false;
            });
            this.CloseQrCommand = new Command(async () => {
                this.IsQrVisible = false;
            });
        }

        void GenerateQr(string codeValue)
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

        internal async void DoBtConnect(BtDeviceEntry btDeviceEntry)
        {
            //TODO
            _app.OperationInProgress = true;
            ServiceContext.Instance.Player.BtConnector.ConnectBt(btDeviceEntry.Info.Address);
            _app.OperationInProgress = false;

            // this.EmulateConnect(btDeviceEntry);
        }

        internal async void DoNetConnect(NetDeviceEntry netDeviceEntry)
        {
            //TODO
            var info = netDeviceEntry.Info;
            _app.OperationInProgress = true;
            ServiceContext.Instance.Player.NetConnector.ConnectTo(info.Address, info.Port);
            _app.OperationInProgress = false;

            // this.EmulateConnect(netDeviceEntry);
        }
    }

}
