using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services.Streams;

namespace MusicShare.Services.NetworkChannels
{

    public class NetworkConnectorService : ServiceWorker<NetworkServieCommand, INetSvcCmdHandler>, INetSvcCmdHandler, INetworkConnector
    {
        private class NetListener : DisposableObject
        {
            public event Action<Socket> OnConnection;
            public event Action<Exception> OnError;

            private readonly DiscoverService _discoverService;
            private readonly TcpListener _listener;

            public NetListener(Guid serviceId, ushort localPort, string localName)
            {
                _listener = TcpListener.Create(localPort);

                _discoverService = new DiscoverService(serviceId, localPort, localName);
            }

            public void Start()
            {
                try
                {
                    _listener.Start();
                    _listener.BeginAcceptSocket(this.OnConnectionProc, null);
                }
                catch (Exception ex) { this.OnError?.Invoke(ex); }
            }

            private void OnConnectionProc(IAsyncResult ar)
            {
                try
                {
                    Socket sck = _listener.EndAcceptSocket(ar);

                    this.OnConnection?.Invoke(sck);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Accepting the Socket");
                }

                if (!this.IsDisposed)
                {
                    try { _listener.BeginAcceptSocket(this.OnConnectionProc, null); }
                    catch (Exception ex) { this.OnError?.Invoke(ex); }
                }
            }

            protected override void DisposeImpl()
            {
                _discoverService.SafeDispose();
                try { _listener.Stop(); }
                catch (Exception ex)
                {
                    Log.Error(ex, "Stopping TcpListener");
                }
            }
        }

        protected override INetSvcCmdHandler Interpreter { get { return this; } }

        public event Action<INetDeviceChannel> OnConnection;
        public event Action<NetHostInfo> OnDiscover;
        public event Action<bool> OnStateChanged;
        public event Action<Exception> OnError;

        private readonly StreamNegotiator _negotiator;
        private readonly Guid _serviceId;
        private readonly ushort _port;
        private readonly string _name;

        private NetListener _listener = null;

        public bool IsEnabled { get; private set; } = false;

        private readonly DiscoverClient _discoverClient;

        public NetworkConnectorService(StreamNegotiator negotiator, Guid serviceId, ushort portToUse, string localName)
        {
            _negotiator = negotiator;
            _serviceId = serviceId;
            _port = portToUse;
            _name = localName;

            _discoverClient = new DiscoverClient(serviceId, portToUse);
            _discoverClient.OnNewResponse += h => this.OnDiscover?.Invoke(h);
        }

        void INetSvcCmdHandler.HandleConnect(ConnectNetworkCmd cmd)
        {
            try
            {
                var sck = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                sck.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
                sck.Connect(cmd.RemoteHost, cmd.RemotePort);

                this.HandleConnection(cmd, sck);
            }
            catch (Exception ex)
            {
                cmd?.OnErrorCallback?.Invoke(ex);
            }
        }

        void INetSvcCmdHandler.HandleEnable(EnableNetworkCmd cmd)
        {
            if (!this.IsEnabled)
            {
                this.IsEnabled = true;
                try
                {
                    _listener = new NetListener(_serviceId, _port, _name);
                    _listener.OnConnection += sck => this.HandleConnection(null, sck);
                    _listener.OnError += ex => this.CleanupListener();
                    _listener.Start();
                    this.OnStateChanged(this.IsEnabled);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Enabling network listener");
                    this.CleanupListener();
                }
            }
        }

        void INetSvcCmdHandler.HandleDisable(DisableNetworkCmd cmd)
        {
            if (this.IsEnabled)
                this.CleanupListener();
        }

        void INetSvcCmdHandler.HandleRefresh(RefreshNetworkCmd cmd)
        {
            _discoverClient.Reset();
        }

        private void HandleConnection(ConnectNetworkCmd cmd, Socket sck)
        {
            sck.NoDelay = true;

            var remoteEp = sck.RemoteEndPoint as IPEndPoint;
            var address = remoteEp?.ToString() ?? sck.RemoteEndPoint.ToString();
            var port = (ushort)(remoteEp?.Port ?? 0);

            var stream = new NetworkStream(sck);

            _negotiator.NegotiateAsync(stream, _serviceId, _name, r => {
                if (r.Successed)
                {
                    var info = new NetHostInfo(r.PeerName, address, r.Ping, port);
                    var cnn = new NetDeviceChannel(info, stream, r.AsyncWriter);
                    this.OnConnection?.Invoke(cnn);
                    cmd?.OnSuccessCallback(cnn);
                }
                else
                {
                    this.OnError?.Invoke(r.Exception);
                    cmd?.OnErrorCallback?.Invoke(r.Exception);
                }
            });
        }

        protected void CleanupListener()
        {
            if (_listener != null)
            {
                _listener.SafeDispose();
                _listener = null;
                this.IsEnabled = false;
                this.OnStateChanged(this.IsEnabled);
            }
        }

        protected override void Cleanup()
        {
            this.CleanupListener();
            _discoverClient.SafeDispose();
        }

        public void Connect(string remoteHost, ushort remotePort, Action<NetDeviceChannel> onSuccess, Action<Exception> onError)
        {
            this.Post(new ConnectNetworkCmd(remoteHost, remotePort, onSuccess, onError));
        }

        public void Enable()
        {
            this.Post(new EnableNetworkCmd());
        }

        public void Disable()
        {
            this.Post(new DisableNetworkCmd());
        }

        public void Refresh()
        {
            this.Post(new RefreshNetworkCmd());
        }
    }
}
