using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Platform;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace MusicShare.Uwp.Services.Platform
{
    internal class PlatformServices : IPlatformServices
    {
        public PlatformServices()
        {

        }

        IBluetoothConnectorImpl IPlatformServices.CreateBluetoothConnector()
        {
            throw new NotImplementedException();
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
