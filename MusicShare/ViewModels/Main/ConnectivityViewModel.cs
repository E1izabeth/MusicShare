using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

        protected DeviceEntry(ConnectivityViewModel owner)
        {
            this.Owner = owner;
        }
    }

    class BtDeviceEntry : DeviceEntry
    {
        public BtDeviceEntryInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        public override string Ping { get { return "[" + this.Info.Address + "]"; } }

        public BtDeviceEntry(ConnectivityViewModel owner, BtDeviceEntryInfo info)
            : base(owner)
        {
            this.Info = info;
        }
    }

    class NetDeviceEntry : DeviceEntry
    {
        public NetHostInfo Info { get; }

        public override string Title { get { return this.Info.Name; } }
        public override string Ping { get { return this.Info.Ping.Truncate(TimeSpan.FromMilliseconds(1)).TotalSeconds + "[" + this.Info.Address + "]"; } }

        public NetDeviceEntry(ConnectivityViewModel owner, NetHostInfo info)
            : base(owner)
        {
            this.Info = info;
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
            if (oldValue is DeviceEntry deselected)
                deselected.IsConnectBtnVisible = false;

            if (newValue is DeviceEntry selected)
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

        public ConnectivityViewModel(AppViewModel app)
            : base("Connectivity")
        {
            this.Devices = new ObservableCollection<DeviceEntry>();

            var player = ServiceContext.Instance.Player;

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
                // TODO generate connectivity0descriptive qr
                this.GenerateQr(Convert.ToBase64String(Enumerable.Range(0, 20).SelectMany(n => Guid.NewGuid().ToByteArray()).ToArray()));
            });
            this.MakeSpotCommand = new Command(async () => {
                // TODO connect by qr
                this.ScanQr();
            });
            this.RefreshDevicesCommand = new Command(async () => {
                this.IsRefreshingDevices = true;

                this.Devices.Clear();
                player.BtConnector.RefreshDevices();
                player.NetConnector.RefreshHosts();

                await Task.Delay(5000);
                this.IsRefreshingDevices = false;
            });
            this.CloseQrCommand = new Command(async () => {
                this.QrContent = null;
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

        private async void ScanQr()
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            var result = await scanner.Scan();
            var text = result.Text;
            Log.Message("QR SCANNED", text);
        }
    }

}
