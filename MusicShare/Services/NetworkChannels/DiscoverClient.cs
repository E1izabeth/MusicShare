using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MusicShare.Services.NetworkChannels
{

    public class DiscoverClient : DisposableObject
    {
        public Guid ServiceId { get; }
        readonly byte[] _serviceIdBytes;

        public const bool LocalOnly = false;

        readonly Dictionary<string, NetHostInfo> _knownSevers = new Dictionary<string, NetHostInfo>();

        ushort _port;
        Socket _sck;
        byte[] _recvBuffer;
        byte[] _sendBuffer;
        DateTime _stamp;

        public event Action<NetHostInfo> OnNewResponse = delegate { };

        public DiscoverClient(Guid serviceId, ushort port)
        {
            this.ServiceId = serviceId;
            _serviceIdBytes = serviceId.ToByteArray();

            _port = port;
            _sendBuffer = serviceId.ToByteArray();

            _recvBuffer = new byte[4096];

            _sck = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            _sck.EnableBroadcast = true;
            _sck.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            _sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            _sck.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));

            try
            {
                EndPoint rep = new IPEndPoint(IPAddress.IPv6Any, 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
            }
            catch (SocketException)
            {
                System.Threading.Thread.Sleep(1000);
                EndPoint rep = new IPEndPoint(IPAddress.IPv6Any, 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
            }
        }

        private void RecvProc(IAsyncResult ar)
        {
            try
            {
                EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
                var l = _sck.EndReceiveFrom(ar, ref ep);

                if ((ep.AddressFamily == AddressFamily.InterNetwork || ep.AddressFamily == AddressFamily.InterNetworkV6)&&
                     l > _serviceIdBytes.Length + 4 && Enumerable.SequenceEqual(_recvBuffer.Take(_serviceIdBytes.Length), _serviceIdBytes))
                {
                    var ipep = (IPEndPoint)ep;

                    lock (_knownSevers)
                    {
                        var addr = ipep.Address.ToString();
                        var port = (ushort)ipep.Port;
                        var key = addr + ":" + port;

                        if (!_knownSevers.ContainsKey(key))
                        {
                            var ping = DateTime.Now - _stamp;
                            var nameLength = BitConverter.ToInt32(_recvBuffer, _serviceIdBytes.Length);
                            var name = Encoding.UTF8.GetString(_recvBuffer, _serviceIdBytes.Length + 4, nameLength);

                            var item = new NetHostInfo(name, addr, ping, port);

                            _knownSevers[key] = item;

                            OnNewResponse(item);
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }

            try
            {
                EndPoint rep = new IPEndPoint(IPAddress.IPv6Any, 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        public void Reset()
        {
            lock (_knownSevers)
            {
                try
                {
                    _knownSevers.Clear();
                    _stamp = DateTime.Now;
                    _sck.SendTo(_sendBuffer, new IPEndPoint(LocalOnly ? IPAddress.Loopback : IPAddress.Broadcast, _port));

                }
                catch (SocketException ex)
                {
                    Debug.Print("DiscoverClient - error with socket: " + ex.ToString());
                }
            }
        }

        protected override void DisposeImpl()
        {
            _sck.SafeDispose();
        }
    }
}
