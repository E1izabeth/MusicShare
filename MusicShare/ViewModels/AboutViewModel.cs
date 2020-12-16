using MusicShare.Net.Discovery;
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
    public interface ILocalUtility
    {
        void Play(string uri);

        Task<BtDeviceEntry[]> DiscoverBt();
        void ListenBt();
        void ConnectBt(string addt);
    }

    public class BtDeviceEntry
    {
        public string Address { get; private set; }
        public string Name { get; private set; }

        public BtDeviceEntry(string address, string name)
        {
            this.Address = address;
            this.Name = name;
        }
    }

    public static class LocalUtilityContext
    {
        public static ILocalUtility Current { get; private set; }

        public static void Set(ILocalUtility ctx)
        {
            Current = ctx;
        }
    }

    public class AboutViewModel : MenuPageViewModel
    {
        public AboutViewModel()
            : base("About")
        {
            this.BtDevices = new ObservableCollection<BtDeviceEntry>();
            this.NetSvcs = new ObservableCollection<ServerItem>();
        
            this.OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
            this.BtListenCmd = new Command(() => LocalUtilityContext.Current.ListenBt());
            this.BtConnectCmd = new Command(() => LocalUtilityContext.Current.ConnectBt(this.SelectedDevc.Address));
            this.BtDiscoverCmd = new Command(async () => {
                this.BtDevices.Clear();
                var devs = await LocalUtilityContext.Current.DiscoverBt();
                devs.ForEach(this.BtDevices.Add);
            });
            _clnt.OnNewResponse += s => this.InvokeAction(() => this.NetSvcs.Add(s));
            this.NetDiscoverRefresh = new Command(() => { this.NetSvcs.Clear(); _clnt.Reset(); });
        }

        public ICommand OpenWebCommand { get; }

        #region ObservableCollection<BtDeviceEntry> BtDevices 

        public ObservableCollection<BtDeviceEntry> BtDevices
        {
            get { return (ObservableCollection<BtDeviceEntry>)this.GetValue(BtDevicesProperty); }
            set { this.SetValue(BtDevicesProperty, value); }
        }

        // Using a BindableProperty as the backing store for BtDevices. This enables animation, styling, binding, etc...
        public static readonly BindableProperty BtDevicesProperty =
            BindableProperty.Create("BtDevices", typeof(ObservableCollection<BtDeviceEntry>), typeof(AboutViewModel), default(ObservableCollection<BtDeviceEntry>));

        #endregion

        #region BtDeviceEntry SelectedDevc 

        public BtDeviceEntry SelectedDevc
        {
            get { return (BtDeviceEntry)this.GetValue(SelectedDevcProperty); }
            set { this.SetValue(SelectedDevcProperty, value); }
        }

        // Using a BindableProperty as the backing store for SelectedDevc. This enables animation, styling, binding, etc...
        public static readonly BindableProperty SelectedDevcProperty =
            BindableProperty.Create("SelectedDevc", typeof(BtDeviceEntry), typeof(AboutViewModel), default(BtDeviceEntry));

        #endregion

        #region ObservableCollection<ServerItem> NetSvcs 

        public ObservableCollection<ServerItem> NetSvcs
        {
            get { return (ObservableCollection<ServerItem>)this.GetValue(NetSvcsProperty); }
            set { this.SetValue(NetSvcsProperty, value); }
        }

        // Using a BindableProperty as the backing store for NetSvcs. This enables animation, styling, binding, etc...
        public static readonly BindableProperty NetSvcsProperty =
            BindableProperty.Create("NetSvcs", typeof(ObservableCollection<ServerItem>), typeof(AboutViewModel), default(ObservableCollection<ServerItem>));

        #endregion

        private readonly DiscoverService _svc = new DiscoverService(11341, "msdss");
        private readonly DiscoverClient _clnt = new DiscoverClient(Guid.NewGuid(), 11341);

        public ICommand BtListenCmd { get; }
        public ICommand BtConnectCmd { get; }
        public ICommand BtDiscoverCmd { get; }
        public ICommand NetDiscoverRefresh { get; }

    }
}