using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MusicShare.Net.Discovery
{
    public class DiscoverService : DisposableObject, IDisposable
    {
        public Guid ServiceId { get; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _sendBuffer = this.ServiceId.ToByteArray().Concat(Encoding.UTF8.GetBytes(value)).ToArray();
                _name = value;
            }
        }

        Socket _sck;
        byte[] _recvBuffer;
        byte[] _sendBuffer;
        byte[] _serviceIdBytes;

        public DiscoverService(Guid serviceId, ushort port, string name)
        {
            this.ServiceId = serviceId;
            this.Name = name;

            _serviceIdBytes = serviceId.ToByteArray();

            _recvBuffer = new Guid().ToByteArray();
            _sck = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            _sck.EnableBroadcast = true;
            _sck.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
            _sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            _sck.Bind(new IPEndPoint(IPAddress.IPv6Any, port));

            EndPoint rep = new IPEndPoint(IPAddress.IPv6Any, 0);
            _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
        }

        private void RecvProc(IAsyncResult ar)
        {
            EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
            try
            {
                var l = _sck.EndReceiveFrom(ar, ref ep);

                if (l == _recvBuffer.Length && Enumerable.SequenceEqual(_recvBuffer, _serviceIdBytes))
                {
                    _sck.SendTo(_sendBuffer, ep);
                }

                EndPoint rep = new IPEndPoint(IPAddress.IPv6Any, 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
            }
            catch (ObjectDisposedException ex) { }
            catch (SocketException) { }
        }

        protected override void DisposeImpl()
        {
            _sck.Dispose();
        }
    }
}
