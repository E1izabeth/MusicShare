using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MusicShare.Net.Discovery
{
    public class DiscoverService : IDisposable
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _sendBuffer = Encoding.UTF8.GetBytes(value);
                _name = value;
            }
        }

        Socket _sck;
        byte[] _recvBuffer;
        byte[] _sendBuffer;

        public DiscoverService(ushort port, string name)
        {
            this.Name = name;

            _recvBuffer = new Guid().ToByteArray();
            _sck = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sck.EnableBroadcast = true;
            _sck.Bind(new IPEndPoint(IPAddress.Any, port));

            EndPoint rep = new IPEndPoint(IPAddress.Any, 0);
            _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, RecvProc, null);
        }

        private void RecvProc(IAsyncResult ar)
        {
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                var l = _sck.EndReceiveFrom(ar, ref ep);

                if (l == _recvBuffer.Length)
                {
                    _sck.SendTo(_sendBuffer, ep);
                }

                EndPoint rep = new IPEndPoint(IPAddress.Any, 0);
                _sck.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, ref rep, RecvProc, null);
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
            }
        }

        public void Dispose()
        {
            _sck.Dispose();
        }
    }
}
