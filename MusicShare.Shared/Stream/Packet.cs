using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MusicShare.Shared.Common;

namespace MusicShare.Interaction.Standard.Stream
{
    public interface IPacketHandler<T>
    {
        T HandleStreamInfo(TrackInfoPacket pckt);
        T HandleStreamHead(StreamDataHeadPacket pckt);
        T HandleStreamBody(StreamDataBodyPacket pckt);
        T HandleStreamTail(StreamDataTailPacket pckt);
        T HandleStreamHead(StampedStreamDataHeadPacket pckt);
        T HandleStreamBody(StampedStreamDataBodyPacket pckt);
        T HandleStreamTail(StampedStreamDataTailPacket pckt);
        T HandleControl(PlaybackControlPacket pckt);
    }

    public abstract class Packet
    {
        protected abstract int PacketKindId { get; }

        private static List<Func<Packet>> _ctors = new List<Func<Packet>>();

        static Packet()
        {
            Register<StampedStreamDataHeadPacket>();
            Register<StampedStreamDataBodyPacket>();
            Register<StampedStreamDataTailPacket>();
            Register<TrackInfoPacket>();
            Register<PlaybackControlPacket>();
        }

        protected static void Register<T>()
            where T : Packet, new()
        {
            var p = new T();
            //var attrs = typeof(T).GetCustomAttributes<PacketIdAttribute>().ToArray();
            //if (attrs.Length == 0)
            //    throw new InvalidOperationException("Trying to register non-packet class as packet");

            //var id = attrs.First().Value;
            var id = p.PacketKindId;
            if (id >= 0)
            {
                while (_ctors.Count <= id)
                    _ctors.Add(null);

                if (_ctors[id] != null)
                    throw new InvalidOperationException($"Packet with id {id} already registered");

                _ctors[id] = () => new T();
            }
        }

        public static Packet Parse(byte[] data, int packetStart, int packetLength)
        {
            var pos = packetStart;
            var length = data.UnpackInt16(ref pos);
            var packetId = data.UnpackInt16(ref pos);
            if (length != packetLength)
                throw new InvalidOperationException("Invalid packet length");

            var ctor = packetId >= 0 && packetId < _ctors.Count ? _ctors[packetId] : null;
            if (ctor == null)
                return null;

            var packet = ctor();
            packet.FromByteArray(data, pos, packetStart + packetLength);
            return packet;
        }

        private void FromByteArray(byte[] data, int pos, int end)
        {
            this.DecodeInternal(data, ref pos);
            if (pos != end)
                throw new InvalidOperationException("Incomplete packet data");
        }

        public T HandleWith<T>(IPacketHandler<T> handler)
        {
            return this.HandleWithImpl<T>(handler);
        }

        protected abstract T HandleWithImpl<T>(IPacketHandler<T> handler);

        public byte[] ToArray()
        {
            var buff = this.EncodeInternal(0, out var pos);
            if (pos != buff.Length)
                throw new InvalidOperationException("Incomplete packet data");

            return buff;
        }

        protected virtual byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var buff = new byte[4 + extraPayloadSize];

            pos = 0;
            buff.PackInt16((short)buff.Length, ref pos);
            buff.PackInt16((short)this.PacketKindId, ref pos);
            return buff;
        }

        protected virtual void DecodeInternal(byte[] data, ref int pos)
        {
        }
    }

    public enum PlaybackControlOperation
    {
        Start,
        Stop
    }

    public class PlaybackControlPacket : Packet
    {
        protected override int PacketKindId { get { return 10; } }

        public PlaybackControlOperation Operation { get; set; }

        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var buff = base.EncodeInternal(2 + extraPayloadSize, out pos);
            buff.PackInt16((short)this.Operation, ref pos);
            return buff;
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            base.DecodeInternal(data, ref pos);
            this.Operation = (PlaybackControlOperation)data.UnpackInt16(ref pos);
        }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleControl(this); }
    }

    public abstract class StreamPacket : Packet
    {
        public int StreamId { get; set; }

        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var buff = base.EncodeInternal(extraPayloadSize + 4, out pos);
            buff.PackInt32(this.StreamId, ref pos);

            return buff;
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            base.DecodeInternal(data, ref pos);

            this.StreamId = data.UnpackInt32(ref pos);
        }
    }

    public class TrackInfoPacket : StreamPacket
    {
        protected override int PacketKindId { get { return 20; } }

        public string TitlePlaying { get; set; }
        public string AuthorPlaying { get; set; }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamInfo(this); }

        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var l1 = Encoding.UTF8.GetByteCount(this.TitlePlaying);
            var l2 = Encoding.UTF8.GetByteCount(this.AuthorPlaying);

            var buff = base.EncodeInternal(extraPayloadSize + l1 + l2 + 2 + 2, out pos);
            buff.PackInt16((short)l1, ref pos);
            pos += Encoding.UTF8.GetBytes(this.TitlePlaying, 0, this.TitlePlaying.Length, buff, pos);
            buff.PackInt16((short)l2, ref pos);
            pos += Encoding.UTF8.GetBytes(this.AuthorPlaying, 0, this.AuthorPlaying.Length, buff, pos);

            return buff;
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            base.DecodeInternal(data, ref pos);

            var l1 = data.UnpackInt16(ref pos);
            this.TitlePlaying = Encoding.UTF8.GetString(data, pos, l1); pos += l1;
            var l2 = data.UnpackInt16(ref pos);
            this.AuthorPlaying = Encoding.UTF8.GetString(data, pos, l2); pos += l2;
        }
    }

    public interface IDataPacketHandler
    {
        void HandleStreamHead(StreamDataHeadPacket packet);
        void HandleStreamBody(StreamDataBodyPacket packet);
        void HandleStreamTail(StreamDataTailPacket packet);
    }

    public abstract class StreamDataPacket : StreamPacket
    {
        protected override int PacketKindId
        {
            get
            {
                return -1;
            }
        }

        public RawData DataFrame { get; set; }

        public void HandleWith(IDataPacketHandler handler)
        {
            this.HandleWithImpl(handler);
        }

        protected abstract void HandleWithImpl(IDataPacketHandler handler);

        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            throw new NotImplementedException("");
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            throw new NotImplementedException("");
        }
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

    public interface IStampedDataPacketHandler
    {
        void HandleStreamHead(StampedStreamDataHeadPacket packet);
        void HandleStreamBody(StampedStreamDataBodyPacket packet);
        void HandleStreamTail(StampedStreamDataTailPacket packet);
    }

    [Flags]
    public enum StampedStreamDataPacketFlags
    {
        None = 0,
        KeyFrame = 1,
        SyncFrame = 1,
        CodecConfig = 2,
        EndOfStream = 4,
        PartialFrame = 8
    }
    public abstract class StampedStreamDataPacket : StreamPacket
    {
        private const int PayloadLength = 14;

        public RawData DataFrame { get; set; }
        public TimeSpan Stamp { get; set; }
        public int Flags { get; set; }

        public StampedStreamDataPacket()
        {
        }

        protected StampedStreamDataPacket(StampedStreamDataPacket other)
        {
            var data = other.DataFrame;
            var buff = new byte[data.Size];
            Array.Copy(data.Data, data.Offset, buff, 0, data.Size);

            this.DataFrame = new RawData(buff, 0, buff.Length);
            this.Stamp = other.Stamp;
            this.Flags = other.Flags;
        }

        public void HandleWith(IStampedDataPacketHandler handler)
        {
            this.HandleWithImpl(handler);
        }

        protected abstract void HandleWithImpl(IStampedDataPacketHandler handler);

        public StampedStreamDataPacket Clone()
        {
            return this.CloneImpl();
        }

        protected abstract StampedStreamDataPacket CloneImpl();

        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var buff = base.EncodeInternal(extraPayloadSize + PayloadLength + this.DataFrame.Size, out pos);
            var data = this.DataFrame;

            buff.PackInt64(this.Stamp.Ticks, ref pos);
            buff.PackInt32(this.Flags, ref pos);
            buff.PackInt16((short)data.Size, ref pos);
            Array.Copy(data.Data, data.Offset, buff, pos, data.Size); pos += data.Size;
            return buff;
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            base.DecodeInternal(data, ref pos);

            this.Stamp = TimeSpan.FromTicks(data.UnpackInt64(ref pos));
            this.Flags = data.UnpackInt32(ref pos);
            var dataLen = data.UnpackInt16(ref pos);

            var buff = new byte[dataLen];
            Array.Copy(data, pos, buff, 0, dataLen); pos += dataLen;
            this.DataFrame = new RawData(buff, 0, dataLen);
        }
    }
    public sealed class StampedStreamDataHeadPacket : StampedStreamDataPacket
    {
        protected override int PacketKindId { get { return 30; } }

        private const int PayloadLength = 6;

        public int SampleRate { get; set; }
        public bool IsMono { get; set; }

        public StampedStreamDataHeadPacket()
        {
        }

        protected StampedStreamDataHeadPacket(StampedStreamDataHeadPacket other)
            : base(other)
        {
            this.SampleRate = other.SampleRate;
            this.IsMono = other.IsMono;
        }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamHead(this); }
        protected override void HandleWithImpl(IStampedDataPacketHandler handler) { handler.HandleStreamHead(this); }
        protected override StampedStreamDataPacket CloneImpl() { return new StampedStreamDataHeadPacket(this); }


        protected override byte[] EncodeInternal(int extraPayloadSize, out int pos)
        {
            var buff = base.EncodeInternal(extraPayloadSize + PayloadLength, out pos);
            buff.PackInt32(this.SampleRate, ref pos);
            buff.PackInt16(this.IsMono ? (byte)0xff : (byte)0x00, ref pos);
            return buff;
        }

        protected override void DecodeInternal(byte[] data, ref int pos)
        {
            base.DecodeInternal(data, ref pos);
            this.SampleRate = data.UnpackInt32(ref pos);
            this.IsMono = data.UnpackInt16(ref pos) == 0xff;
        }
    }
    public sealed class StampedStreamDataBodyPacket : StampedStreamDataPacket
    {
        protected override int PacketKindId { get { return 31; } }

        public StampedStreamDataBodyPacket()
        {
        }

        protected StampedStreamDataBodyPacket(StampedStreamDataBodyPacket other)
            : base(other)
        {
        }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamBody(this); }
        protected override void HandleWithImpl(IStampedDataPacketHandler handler) { handler.HandleStreamBody(this); }
        protected override StampedStreamDataPacket CloneImpl() { return new StampedStreamDataBodyPacket(this); }
    }
    public sealed class StampedStreamDataTailPacket : StampedStreamDataPacket
    {
        protected override int PacketKindId { get { return 32; } }

        public StampedStreamDataTailPacket()
        {
        }

        protected StampedStreamDataTailPacket(StampedStreamDataTailPacket other)
            : base(other)
        {
        }

        protected override T HandleWithImpl<T>(IPacketHandler<T> handler) { return handler.HandleStreamTail(this); }
        protected override void HandleWithImpl(IStampedDataPacketHandler handler) { handler.HandleStreamTail(this); }
        protected override StampedStreamDataPacket CloneImpl() { return new StampedStreamDataTailPacket(this); }
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
