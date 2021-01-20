using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MusicShare.Interaction.Standard.Common;

namespace MusicShare.Services.NetworkChannels
{
    public class DiscoverService : DisposableObject, IDisposable
    {
        private class Context : DisposableObject
        {
            private readonly DiscoverService _owner;
            private readonly Socket _sck;
            private readonly byte[] _recvBuffer;
            private readonly IPAddress _anyAddress;

            public Context(DiscoverService owner, IPAddress anyAddress, ushort port)
            {
                _owner = owner;
                _anyAddress = anyAddress;

                _recvBuffer = new Guid().ToByteArray();
                _sck = new Socket(anyAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _sck.EnableBroadcast = true;
                //_sck.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                _sck.Bind(new IPEndPoint(anyAddress.Clone(), port));

                EndPoint rep = new IPEndPoint(anyAddress.Clone(), 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, this.RecvProc, null);
            }

            private void RecvProc(IAsyncResult ar)
            {
                EndPoint ep = new IPEndPoint(_anyAddress.Clone(), 0);
                try
                {
                    var l = _sck.EndReceiveFrom(ar, ref ep);

                    if (l == _recvBuffer.Length && Enumerable.SequenceEqual(_recvBuffer, _owner._serviceIdBytes))
                    {
                        _sck.SendTo(_owner._sendBuffer, ep);
                    }

                    EndPoint rep = new IPEndPoint(_anyAddress.Clone(), 0);
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

        public Guid ServiceId { get; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                _sendBuffer = this.ServiceId.ToByteArray().Concat(BitConverter.GetBytes(bytes.Length)).Concat(bytes).ToArray();
                _name = value;
            }
        }

        private byte[] _sendBuffer;
        private byte[] _serviceIdBytes;
        private readonly Context _v4, _v6;

        public DiscoverService(Guid serviceId, ushort port, string name)
        {
            this.ServiceId = serviceId;
            this.Name = name;

            _serviceIdBytes = serviceId.ToByteArray();

            _v4 = new Context(this, IPAddress.Any, port);
            _v6 = new Context(this, IPAddress.IPv6Any, port);
        }
        
        protected override void DisposeImpl()
        {
            _v4.SafeDispose();
            _v6.SafeDispose();
        }
    }
}
