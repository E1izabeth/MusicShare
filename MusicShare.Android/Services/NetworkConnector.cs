using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using MusicShare.Net.Discovery;
using MusicShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MusicShare.Droid.Services
{
    class NetDeviceConnectionAsyncSender
    {
        public Stream Stream { get; }

        private readonly object _lock = new object();
        private readonly Queue<RawData> _queue = new Queue<RawData>();
        private bool _sending = false;
        private AsyncCallback _writeProc;
        private volatile bool _broken = false;

        public NetDeviceConnectionAsyncSender(Stream stream)
        {
            this.Stream = stream;
            _writeProc = this.WriteProc;
        }

        public void SendAsync(byte[] data)
        {
            this.SendAsync(data, 0, data.Length);
        }

        public void SendAsync(byte[] data, int offset, int size)
        {
            if (_broken)
                throw new InvalidOperationException();

            lock (_lock)
            {
                if (_sending)
                {
                    _queue.Enqueue(new RawData(data, offset, size));
                }
                else
                {
                    _sending = true;

                    try { this.Stream.BeginWrite(data, offset, size, _writeProc, null); }
                    catch { _broken = true; }
                }

                if (_broken)
                {
                    _queue.Clear();
                }
            }
        }

        private void WriteProc(IAsyncResult ar)
        {
            try { this.Stream.EndWrite(ar); }
            catch { _broken = true; }

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    if (_broken)
                    {
                        _queue.Clear();
                    }
                    else
                    {
                        var data = _queue.Dequeue();

                        try { this.Stream.BeginWrite(data.Data, data.Offset, data.Size, _writeProc, null); }
                        catch { _broken = true; }
                    }
                }
                else
                {
                    _sending = false;
                }
            }
        }
    }

    class NetDeviceConnection
    {
        public NetHostInfo Info { get; }
        public Stream Stream { get; }
        public NetDeviceConnectionAsyncSender AsyncSender { get; }

        public NetDeviceConnection(NetHostInfo info, Stream stream, NetDeviceConnectionAsyncSender asyncSender)
        {
            this.Info = info;
            this.Stream = stream;
            this.AsyncSender = asyncSender;
        }
    }

    class NetworkConnector : DisposableObject, INetworkConnector
    {
        readonly Random _rnd = new Random();

        public event Action OnDiscoverReset;
        public event Action<NetHostInfo> OnDiscoverFound;

        public event Action<NetDeviceConnection> OnNewConnection;

        static readonly Guid _netSericeId = Guid.Parse("EDD0608E-BA9B-497B-82A7-A2F79D5FC346");
        static readonly byte[] _netServiceIdBytes = _netSericeId.ToByteArray();

        readonly Dictionary<string, NetHostInfo> _knownDevices = new Dictionary<string, NetHostInfo>();

        public bool IsEnabled { get; private set; } = false;

        readonly ManualResetEvent _disposingEv = new ManualResetEvent(false);
        readonly ManualResetEvent _acceptingThreadEv = new ManualResetEvent(false);
        readonly Thread _acceptingThread;

        readonly DiscoverService _discoverService;
        readonly DiscoverClient _discoverClient;

        readonly ushort _port;
        readonly string _name;
        readonly byte[] _connectionHeaderBytes;


        readonly object _pendingConnectionsLock = new object();
        readonly Dictionary<string, Stream> _pendingConnections = new Dictionary<string, Stream>();
        int _connectionsCount = 0;

        public NetworkConnector(Service service, ushort portToUse, string name)
        {
            _discoverService = new DiscoverService(_netSericeId, portToUse, name);
            _discoverClient = new DiscoverClient(_netSericeId, portToUse);
            _discoverClient.OnNewResponse += h => this.OnDiscoverFound?.Invoke(h);

            _port = portToUse;
            _name = name;

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            _connectionHeaderBytes = _netServiceIdBytes.Concat(BitConverter.GetBytes(nameBytes.Length)).Concat(nameBytes).ToArray();

            _acceptingThread = new Thread(this.AcceptingThreadProc);
            _acceptingThread.Start();
        }

        protected override void DisposeImpl()
        {
            _discoverClient.SafeDispose();
            _discoverService.SafeDispose();
            _disposingEv.Set();
        }

        public void RefreshHosts()
        {
            this.OnDiscoverReset?.Invoke();
            _discoverClient.Reset();
        }

        private void AcceptingThreadProc()
        {
            var listener = new TcpListener(_port);
            try
            {
                while (WaitHandle.WaitAny(new[] { _acceptingThreadEv, _disposingEv }) == 0)
                {
                    Socket sck;
                    try { sck = listener.AcceptSocket(); }
                    catch (Exception) { sck = null; }

                    if (sck != null)
                    {
                        this.NegotiateConnection(sck);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private bool NegotiateConnection(Socket sck)
        {
            sck.NoDelay = true;

            var ipep = sck.RemoteEndPoint as IPEndPoint;
            var address = ipep?.Address?.ToString() ?? string.Empty;
            var port = (ushort)(ipep?.Port ?? 0);
            var key = Interlocked.Increment(ref _connectionsCount) + "|" + address + ":" + port + "@" + DateTime.Now;

            var stream = new NetworkStream(sck);
            lock (_pendingConnectionsLock)
            {
                _pendingConnections.Add(key, stream);
            }

            var cookie = _rnd.Next();
            var stamp = DateTime.Now;

            var sender = new NetDeviceConnectionAsyncSender(stream);

            var sendBuff = _connectionHeaderBytes.Concat(BitConverter.GetBytes(cookie)).ToArray();
            try
            {
                sender.SendAsync(sendBuff);

                var buff = new byte[_netServiceIdBytes.Length];
                //stream.BeginRead(buff, 0, buff.Length, ar => {
                if (stream.TryRead(buff))
                {
                    try
                    {
                        // var read = stream.EndRead(ar);
                        int read = buff.Length;
                        if (read == _netServiceIdBytes.Length && Enumerable.SequenceEqual(buff, _netServiceIdBytes))
                        {
                            var nameLenBytes = new byte[4];
                            if (stream.TryRead(nameLenBytes))
                            {
                                var nameBytes = new byte[BitConverter.ToInt32(nameLenBytes)];
                                if (stream.TryRead(nameBytes))
                                {
                                    var cookieBuff = new byte[4];
                                    if (stream.TryRead(cookieBuff))
                                    {
                                        sender.SendAsync(cookieBuff);
                                        if (stream.TryRead(cookieBuff))
                                        {
                                            var cookieRcvd = BitConverter.ToInt32(cookieBuff);
                                            if (cookieRcvd == cookie)
                                            {
                                                var name = System.Text.Encoding.UTF8.GetString(nameBytes);
                                                var info = new NetHostInfo(name, address, DateTime.Now - stamp, port);
                                                this.OnNewConnection(new NetDeviceConnection(info, stream, sender));
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to negotiate connection " + key);
                    }
                    finally
                    {
                        lock (_pendingConnectionsLock)
                        {
                            _pendingConnections.Remove(key);
                        }
                    }
                }
                else
                {
                    lock (_pendingConnectionsLock)
                    {
                        _pendingConnections.Remove(key);
                    }
                }
                //}, null);
            }
            catch (Exception)
            {
                lock (_pendingConnectionsLock)
                {
                    _pendingConnections.Remove(key);
                }
            }

            return false;
        }

        public bool ConnectTo(string host, ushort port)
        {
            try
            {
                var sck = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                sck.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
                sck.Connect(host, port);
                return this.NegotiateConnection(sck);
            }
            catch (Exception ex)
            {
                Log.TraceMethod("Unable to establish network connection: " + ex.ToString());
                return false;
            }
        }
    }
}