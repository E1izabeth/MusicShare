using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MusicShare.Interaction.Standard.Common;

namespace MusicShare.Services.Streams
{
    public class StreamNegotiator : ServiceWorker<StreamNegotiatorCommand, IStreamNegotiatorCommandHandler>, IStreamNegotiatorCommandHandler
    {
        protected override IStreamNegotiatorCommandHandler Interpreter { get { return this; } }

        private readonly object _lock = new object();
        private readonly Random _rnd = new Random();

        //private readonly Thread _thread;
        //private readonly AsyncContextsDispatcher _dispatcher = new AsyncContextsDispatcher();

        //public StreamNegotiator()
        //{
        //    _thread = new Thread(this.WorkerProc) {
        //        IsBackground = true
        //    };
        //    _thread.Start();
        //}

        //private void WorkerProc()
        //{
        //    _dispatcher.DoWork();
        //}

        //private IEnumerable<AsyncStep> NegotiateConnection(NegotiateAsyncCommand cmd)
        //{
        //    var stream = cmd.Stream;
        //    var stamp = DateTime.Now;

        //    // var sender = new StreamAsyncSender(stream);

        //    var netServiceIdBytes = cmd.ServiceId.ToByteArray();
        //    var localNameBytes = Encoding.UTF8.GetBytes(cmd.LocalPeerName);
        //    var cookie = _rnd.Next();
        //    var sendBuff = netServiceIdBytes.Concat(BitConverter.GetBytes(localNameBytes.Length))
        //                                    .Concat(localNameBytes)
        //                                    .Concat(BitConverter.GetBytes(cookie)).ToArray();

        //    yield return stream.WriteAsyncStep(sendBuff); // send serviceId, localNameLength, localNameBytes, localCookie

        //    var buff = new byte[netServiceIdBytes.Length];
        //    yield return stream.ReadAsyncStep(buff); // receive remote serviceId

        //    if (!Enumerable.SequenceEqual(buff, netServiceIdBytes)) // same service on both sides
        //        throw new ApplicationException("Channel Service Identifier acknowledgement failed");

        //    var nameLenBytes = new byte[4];
        //    yield return stream.ReadAsyncStep(nameLenBytes); // receive remoteNameLength

        //    var remoteNameLength = BitConverter.ToInt32(nameLenBytes, 0);
        //    if (remoteNameLength > 1024)
        //        throw new ApplicationException("Remote peer name length is out of range");

        //    var remoteNameBytes = new byte[remoteNameLength];
        //    yield return stream.ReadAsyncStep(remoteNameBytes); // receive remoteNameBytes

        //    var cookieBuff = new byte[4];
        //    yield return stream.ReadAsyncStep(cookieBuff); // receive remoteCookie

        //    yield return stream.WriteAsyncStep(cookieBuff); // send remoteCookie back

        //    yield return stream.ReadAsyncStep(cookieBuff); // receive localCookie back

        //    var cookieRcvd = BitConverter.ToInt32(cookieBuff, 0);
        //    if (cookieRcvd != cookie) // cookie matched
        //        throw new ApplicationException("Channel negotiation cookie acknowledgement is failed");

        //    var remoteName = System.Text.Encoding.UTF8.GetString(remoteNameBytes);
        //    var timeSpent = DateTime.Now - stamp;

        //    cmd.Callback(NegotiateAsyncCommand.ResultInfo.ForSuccess(cmd.Stream, timeSpent));
        //}

        private readonly WorkerThreadPool _workers = new WorkerThreadPool(3);

        public StreamNegotiator()
        {

        }

        private int NextCookie()
        {
            lock (_lock)
            {
                return _rnd.Next();
            }
        }

        private void NegotiateConnection(NegotiateAsyncCommand cmd)
        {

            var stream = cmd.Stream;
            var sender = new StreamAsyncWriter(stream);

            var netServiceIdBytes = cmd.ServiceId.ToByteArray();
            var localNameBytes = Encoding.UTF8.GetBytes(cmd.LocalPeerName);
            var cookie = this.NextCookie();
            var sendBuff = netServiceIdBytes.Concat(BitConverter.GetBytes(localNameBytes.Length))
                                            .Concat(localNameBytes)
                                            .Concat(BitConverter.GetBytes(cookie)).ToArray();

            var stamp = DateTime.Now;
            try
            {
                sender.SendAsync(sendBuff);

                var buff = new byte[netServiceIdBytes.Length];
                if (!stream.TryRead(buff))
                    throw new ApplicationException("Failed to retrieve service Id");

                if (!Enumerable.SequenceEqual(buff, netServiceIdBytes))
                    throw new ApplicationException("Service Id is invalid");

                var nameLenBytes = new byte[4];
                if (!stream.TryRead(nameLenBytes))
                    throw new ApplicationException("Failed to retrieve peer name length");

                var nameBytes = new byte[BitConverter.ToInt32(nameLenBytes, 0)];
                if (!stream.TryRead(nameBytes))
                    throw new ApplicationException("Failed to retrieve peer name");

                var cookieBuff = new byte[4];
                if (!stream.TryRead(cookieBuff))
                    throw new ApplicationException("Failed to retrieve cookie request");

                sender.SendAsync(cookieBuff);

                if (!stream.TryRead(cookieBuff))
                    throw new ApplicationException("Failed to retrieve cookie response");

                var cookieRcvd = BitConverter.ToInt32(cookieBuff, 0);
                if (cookieRcvd != cookie)
                    throw new ApplicationException("Cookie negotiation failed");

                var name = System.Text.Encoding.UTF8.GetString(nameBytes);
                cmd.Callback(NegotiateAsyncCommand.ResultInfo.ForSuccess(stream, sender, DateTime.Now - stamp, name));
            }
            catch (Exception ex)
            {
                cmd.Callback(NegotiateAsyncCommand.ResultInfo.ForException(ex));
            }
        }

        #region handlers

        void IStreamNegotiatorCommandHandler.DoNegotiateAsync(NegotiateAsyncCommand cmd)
        {
            // _dispatcher.PostWork(new AsyncStep(this.NegotiateConnection(cmd), ex => cmd.Callback(NegotiateAsyncCommand.ResultInfo.ForException(ex))));

            _workers.Schedule(() => this.NegotiateConnection(cmd));
        }

        #endregion

        protected override void Cleanup()
        {
            _workers.SafeDispose();
        }

        #region methods

        public void NegotiateAsync(Stream stream, Guid serviceId, string localPeerName, Action<NegotiateAsyncCommand.ResultInfo> callback)
        {
            this.Post(new NegotiateAsyncCommand(stream, serviceId, localPeerName, callback));
        }

        #endregion
    }
}
