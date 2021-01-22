using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Platform;
using MusicShare.Services.Streams;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Popups;

namespace MusicShare.Uwp.Services.Platform
{
    internal class PlatformServices : IPlatformServices
    {
        private class FsFile : IPlatformFsItem
        {
            public StorageFile StorageFile { get; }

            public string Name { get { return this.StorageFile.DisplayName; } }
            public string Path { get { return this.StorageFile.Path; } }
            public bool IsDir { get { return false; } }

            public FsFile(StorageFile file)
            {
                this.StorageFile = file;
            }

            public IPlatformFsItem[] GetDirs()
            {
                return new IPlatformFsItem[0];
            }

            public IPlatformFsItem[] GetFiles()
            {
                return new IPlatformFsItem[0];
            }
        }

        private class FsFolder : IPlatformFsItem
        {
            public StorageFolder StorageFolder { get; }
            public string Name { get { return this.StorageFolder.DisplayName; } }
            public string Path { get { return this.StorageFolder.Path; } }
            public bool IsDir { get { return true; } }

            public FsFolder(StorageFolder storageFolder)
            {
                this.StorageFolder = storageFolder;
            }

            public IPlatformFsItem[] GetDirs()
            {
                return this.StorageFolder.GetFoldersAsync().GetAwaiter().GetResult().Select(f => new FsFolder(f)).ToArray();
            }

            public IPlatformFsItem[] GetFiles()
            {
                return this.StorageFolder.GetFilesAsync().GetAwaiter().GetResult().Select(f => new FsFile(f)).ToArray();
            }
        }

        public PlatformServices()
        {
            PlatformContext.SetInstance(this);
        }

        IBluetoothConnectorImpl IPlatformServices.CreateBluetoothConnector(StreamNegotiator negotiator, Guid serviceId, string localName)
        {
            throw new NotImplementedException();
        }

        IPlatformFsItem[] IPlatformServices.GetFsRoots()
        {
            try
            {
                var root = Path.GetPathRoot(Environment.SystemDirectory);
                StorageFolder folder = StorageFolder.GetFolderFromPathAsync(root).GetAwaiter().GetResult();
            }
            catch
            {
                MessageDialog dlg = new MessageDialog(
                    "It seems you have not granted permission for this app to access the file system broadly. " +
                    "Without this permission, the app will only be able to access a very limited set of filesystem locations. " +
                    "You can grant this permission in the Settings app, if you wish. You can do this now or later. " +
                    "If you change the setting while this app is running, it will terminate the app so that the " +
                    "setting can be applied. Do you want to do this now?",
                    "File system permissions");
                dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(this.InitMessageDialogHandler), 0));
                dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler(this.InitMessageDialogHandler), 1));
                dlg.DefaultCommandIndex = 0;
                dlg.CancelCommandIndex = 1;
                dlg.ShowAsync().GetAwaiter().GetResult();
            }

            return Environment.GetLogicalDrives().Select(d => {
                try { return new FsFolder(StorageFolder.GetFolderFromPathAsync(d).GetAwaiter().GetResult()); }
                catch { return null; }
            }).Where(d => d != null).ToArray();
        }

        async void IPlatformServices.DemandFsPermission()
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(@"C:\");

                // do work
            }
            catch
            {
                MessageDialog dlg = new MessageDialog(
                    "It seems you have not granted permission for this app to access the file system broadly. " +
                    "Without this permission, the app will only be able to access a very limited set of filesystem locations. " +
                    "You can grant this permission in the Settings app, if you wish. You can do this now or later. " +
                    "If you change the setting while this app is running, it will terminate the app so that the " +
                    "setting can be applied. Do you want to do this now?",
                    "File system permissions");
                dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(this.InitMessageDialogHandler), 0));
                dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler(this.InitMessageDialogHandler), 1));
                dlg.DefaultCommandIndex = 0;
                dlg.CancelCommandIndex = 1;
                await dlg.ShowAsync();
            }
        }

        private async void InitMessageDialogHandler(IUICommand command)
        {
            if ((int)command.Id == 0)
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
            }
        }

        //IPAddress[] IPlatformServices.GetLocalAddresses()
        //{
        //    var connectedProfiles = NetworkInformation.FindConnectionProfilesAsync(new ConnectionProfileFilter() { IsConnected = true }).GetAwaiter().GetResult();
        //    var adapterIds = connectedProfiles.Select(p => p?.NetworkAdapter?.NetworkAdapterId).Where(g => g.HasValue).Select(g => g.Value).Where(g => g != Guid.Empty).ToArray();

        //    var hostnames = NetworkInformation.GetHostNames();
        //    var ips = hostnames.Where(h => h.Type == HostNameType.Ipv4 || h.Type == HostNameType.Ipv6 && adapterIds.Contains(h.IPInformation?.NetworkAdapter?.NetworkAdapterId ?? Guid.Empty))
        //                       .Select(h => IPAddress.Parse(h.CanonicalName))
        //                       .ToArray();

        //    return ips;
        //}

        //IPAddress[] IPlatformServices.GetBroadcastAddresses()
        //{
        //    var adapters = Windows.Devices.WiFi.WiFiAdapter.FindAllAdaptersAsync().GetAwaiter().GetResult();
        //    adapters.Where(a => a.)
        //    var filter = new ConnectionProfileFilter() {
        //        IsConnected = true,
        //        IsWlanConnectionProfile = true,
        //        IsWwanConnectionProfile = true,
        //    };

        //    var profiles = NetworkInformation.FindConnectionProfilesAsync(filter).GetAwaiter().GetResult();
        //    var addresses = profiles.SelectMany(p => new[]{
        //        p.GetAttributedNetworkUsageAsync
        //    }).ToArray();

        //    var hostnames = NetworkInformation.GetHostNames();

        //    var ips = hostnames.Where(h => h.Type == HostNameType.Ipv4);
        //}
        //    // the ip address
        //    return hostname?.CanonicalName;
        //}

        string[] IPlatformServices.PickMusicFiles()
        {
            throw new NotImplementedException();
        }
    }
}
