using MusicShare.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public class AboutViewModel : MenuPageViewModel
    {
        public AboutViewModel(AppViewModel app)
            : base("About")
        {
            //this.BtDevices = new ObservableCollection<BtDeviceEntryInfo>();
            //this.NetSvcs = new ObservableCollection<NetHostInfo>();

            //this.OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
            //this.BtListenCmd = new Command(() => LocalUtilityContext.Current.ListenBt());
            //this.BtConnectCmd = new Command(() => LocalUtilityContext.Current.ConnectBt(this.SelectedDevc.Address));
            //this.BtDiscoverCmd = new Command(async () => {
            //    this.BtDevices.Clear();
            //    var devs = await LocalUtilityContext.Current.DiscoverBt();
            //    devs.ForEach(this.BtDevices.Add);
            //});
            //_clnt.OnNewResponse += s => this.InvokeAction(() => this.NetSvcs.Add(s));
            //this.NetDiscoverRefresh = new Command(() => { this.NetSvcs.Clear(); _clnt.Reset(); });
        }

        public ICommand OpenWebCommand { get; }

        //#region ObservableCollection<BtDeviceEntry> BtDevices 

        //public ObservableCollection<BtDeviceEntryInfo> BtDevices
        //{
        //    get { return (ObservableCollection<BtDeviceEntryInfo>)this.GetValue(BtDevicesProperty); }
        //    set { this.SetValue(BtDevicesProperty, value); }
        //}

        //// Using a BindableProperty as the backing store for BtDevices. This enables animation, styling, binding, etc...
        //public static readonly BindableProperty BtDevicesProperty =
        //    BindableProperty.Create("BtDevices", typeof(ObservableCollection<BtDeviceEntryInfo>), typeof(AboutViewModel), default(ObservableCollection<BtDeviceEntryInfo>));

        //#endregion

        //#region BtDeviceEntry SelectedDevc 

        //public BtDeviceEntryInfo SelectedDevc
        //{
        //    get { return (BtDeviceEntryInfo)this.GetValue(SelectedDevcProperty); }
        //    set { this.SetValue(SelectedDevcProperty, value); }
        //}

        //// Using a BindableProperty as the backing store for SelectedDevc. This enables animation, styling, binding, etc...
        //public static readonly BindableProperty SelectedDevcProperty =
        //    BindableProperty.Create("SelectedDevc", typeof(BtDeviceEntryInfo), typeof(AboutViewModel), default(BtDeviceEntryInfo));

        //#endregion

        //#region ObservableCollection<ServerItem> NetSvcs 

        //public ObservableCollection<ServerItem> NetSvcs
        //{
        //    get { return (ObservableCollection<ServerItem>)this.GetValue(NetSvcsProperty); }
        //    set { this.SetValue(NetSvcsProperty, value); }
        //}

        //// Using a BindableProperty as the backing store for NetSvcs. This enables animation, styling, binding, etc...
        //public static readonly BindableProperty NetSvcsProperty =
        //    BindableProperty.Create("NetSvcs", typeof(ObservableCollection<ServerItem>), typeof(AboutViewModel), default(ObservableCollection<ServerItem>));

        //#endregion

        //private readonly DiscoverService _svc = new DiscoverService(11341, "msdss");
        //private readonly DiscoverClient _clnt = new DiscoverClient(Guid.NewGuid(), 11341);

        public ICommand BtListenCmd { get; }
        public ICommand BtConnectCmd { get; }
        public ICommand BtDiscoverCmd { get; }
        public ICommand NetDiscoverRefresh { get; }

        public ICommand StartSvc { get; }
        public ICommand TerminateSvc { get; }

        public ICommand StartCmd { get; }
        public ICommand PauseCmd { get; }
        public ICommand StopCmd { get; }
    }
}