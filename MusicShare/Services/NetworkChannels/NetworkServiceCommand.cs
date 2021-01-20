using System;
using System.Collections.Generic;
using System.Text;

namespace MusicShare.Services.NetworkChannels
{
    public interface INetSvcCmdHandler
    {
        void HandleConnect(ConnectNetworkCmd cmd);
        void HandleEnable(EnableNetworkCmd cmd);
        void HandleDisable(DisableNetworkCmd cmd);
        void HandleRefresh(RefreshNetworkCmd cmd);
    }

    public abstract class NetworkServieCommand : ServiceCommand<NetworkServieCommand, INetSvcCmdHandler>
    {
    }

    public class EnableNetworkCmd : NetworkServieCommand
    {
        protected override void HandleWithImpl(INetSvcCmdHandler svc) { svc.HandleEnable(this); }
    }

    public class DisableNetworkCmd : NetworkServieCommand
    {
        protected override void HandleWithImpl(INetSvcCmdHandler svc) { svc.HandleDisable(this); }
    }

    public class RefreshNetworkCmd : NetworkServieCommand
    {
        protected override void HandleWithImpl(INetSvcCmdHandler svc) { svc.HandleRefresh(this); }
    }

    public class ConnectNetworkCmd : NetworkServieCommand
    {
        public string RemoteHost { get; }
        public ushort RemotePort { get; }
        public Action<NetDeviceChannel> OnSuccessCallback { get; }
        public Action<Exception> OnErrorCallback { get; }

        public ConnectNetworkCmd(string remoteHost, ushort remotePort, Action<NetDeviceChannel> onSuccess, Action<Exception> onError)
        {
            this.RemoteHost = remoteHost;
            this.RemotePort = remotePort;
            this.OnSuccessCallback = onSuccess;
            this.OnErrorCallback = onError;
        }

        protected override void HandleWithImpl(INetSvcCmdHandler svc) { svc.HandleConnect(this); }
    }
}
