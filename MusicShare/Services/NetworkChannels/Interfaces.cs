using System;
using System.Collections.Generic;
using System.Text;

namespace MusicShare.Services.NetworkChannels
{
    public class NetHostInfo
    {
        public string Name { get; }
        public string Address { get; }
        public TimeSpan Ping { get; }
        public ushort Port { get; }

        public NetHostInfo(string name, string address, TimeSpan ping, ushort port)
        {
            this.Name = name;
            this.Address = address;
            this.Ping = ping;
            this.Port = port;
        }
    }

    public interface INetworkConnector : IDisposable
    {
        event Action<INetDeviceChannel> OnConnection;
        event Action<NetHostInfo> OnDiscover;
        event Action<bool> OnStateChanged;
        event Action<Exception> OnError;

        bool IsEnabled { get; }

        void Connect(string remoteHost, ushort remotePort, Action<NetDeviceChannel> onSuccess, Action<Exception> onError);
        void Enable();
        void Disable();
        void Refresh();
    }

}
