using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard.Stream
{
    public interface IPacketHandler<T>
    {
        T HandleStreamControl(StreamControlPacket pckt);
        T HandleStreamHead(StreamDataHeadPacket pckt);
        T HandleStreamBody(StreamDataBodyPacket pckt);
        T HandleStreamTail(StreamDataTailPacket pckt);
    }

    public abstract class Packet
    {
        public T HandleWith<T>(IPacketHandler<T> handler)
        {
            return this.HandleWithImpl<T>(handler);
        }

        protected abstract T HandleWithImpl<T>(IPacketHandler<T> handler);
    }

    public abstract class StreamPacket : Packet
    {
        public int StreamId { get; set; }
    }

    public class StreamControlPacket : StreamPacket
    {
        public string TitlePlaying { get; set; }
        public string AuthorPlaying { get; set; }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamControl(this); }
    }

    public interface IDataPacketHandler
    {
        void HandleStreamHead(StreamDataHeadPacket packet);
        void HandleStreamBody(StreamDataBodyPacket packet);
        void HandleStreamTail(StreamDataTailPacket packet);
    }

    public abstract class StreamDataPacket : StreamPacket
    {
        public RawData DataFrame { get; set; }

        public void HandleWith(IDataPacketHandler handler)
        {
            this.HandleWithImpl(handler);
        }

        protected abstract void HandleWithImpl(IDataPacketHandler handler);
    }
    public class StreamDataHeadPacket : StreamDataPacket
    {
        public long TotalSize { get; set; }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamHead(this); }
        protected override void HandleWithImpl(IDataPacketHandler handler) { handler.HandleStreamHead(this); }
    }
    public class StreamDataBodyPacket : StreamDataPacket
    {
        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamBody(this); }
        protected override void HandleWithImpl(IDataPacketHandler handler) { handler.HandleStreamBody(this); }
    }
    public class StreamDataTailPacket : StreamDataPacket
    {
        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamTail(this); }
        protected override void HandleWithImpl(IDataPacketHandler handler) { handler.HandleStreamTail(this); }
    }

    public class RawData
    {
        public byte[] Data { get; private set; }
        public int Offset { get; private set; }
        public int Size { get; private set; }

        public RawData(byte[] data, int offset, int size)
        {
            this.Data = data;
            this.Offset = offset;
            this.Size = size;
        }
    }

    //public class RawDataFrame : RawData
    //{
    //    public long DurationMills { get; private set; }

    //    public RawDataFrame(byte[] data, int offset, int size, long durationMills)
    //        : base(data, offset, size)
    //    {
    //        this.DurationMills = durationMills;
    //    }
    //}

}
