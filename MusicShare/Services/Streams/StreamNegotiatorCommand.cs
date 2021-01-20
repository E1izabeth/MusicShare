using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicShare.Services.Streams
{
    public interface IStreamNegotiatorCommandHandler
    {
        void DoNegotiateAsync(NegotiateAsyncCommand cmd);
    }

    public abstract class StreamNegotiatorCommand : ServiceCommand<StreamNegotiatorCommand, IStreamNegotiatorCommandHandler>
    {
    }

    public class NegotiateAsyncCommand : StreamNegotiatorCommand
    {
        public class ResultInfo
        {
            public bool Successed { get; }

            public Stream Stream { get; }
            public StreamAsyncWriter AsyncWriter { get; }
            public TimeSpan Ping { get; }
            public string PeerName { get; }

            public Exception Exception { get; }
            public string Message { get; }

            public ResultInfo(Exception exception, string message)
            {
                this.Successed = false;
                this.Exception = exception;
                this.Message = message ?? exception.Message;
            }

            public ResultInfo(Stream stream, StreamAsyncWriter writer, TimeSpan timeSpent, string peerName)
            {
                this.Successed = true;
                this.Stream = stream;
                this.AsyncWriter = writer;
                this.Ping = timeSpent;
                this.PeerName = peerName;
            }

            public static ResultInfo ForException(Exception ex)
            {
                return new ResultInfo(ex, ex.GetInnerException().Message);
            }

            public static ResultInfo ForSuccess(Stream stream, StreamAsyncWriter asyncWriter, TimeSpan timeSpent, string peerName)
            {
                return new ResultInfo(stream, asyncWriter, timeSpent, peerName);
            }
        }

        public Stream Stream { get; }

        public Guid ServiceId { get; }
        public string LocalPeerName { get; }

        public Action<ResultInfo> Callback { get; }

        public NegotiateAsyncCommand(Stream stream, Guid serviceId, string localPeerName, Action<ResultInfo> callback)
        {
            this.Stream = stream;
            this.ServiceId = serviceId;
            this.LocalPeerName = localPeerName;
            this.Callback = callback;
        }

        protected override void HandleWithImpl(IStreamNegotiatorCommandHandler svc) { svc.DoNegotiateAsync(this); }
    }

}
