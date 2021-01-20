using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MusicShare.Uwp.Services.Bluetooth
{
    internal class BluetoothStream : Stream
    {

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get => throw new NotSupportedException(); }
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private DataReader _reader;
        private DataWriter _writer;
        private StreamSocket _sck;

        public BluetoothStream(StreamSocket sck)
        {
            _sck = sck;
            _reader = new DataReader(sck.InputStream);
            _writer = new DataWriter(sck.OutputStream);
        }

        public override void Flush()
        {
            _writer.FlushAsync().GetAwaiter().GetResult();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _reader.LoadAsync((uint)count).GetAwaiter().GetResult();
            if (offset == 0 && count == buffer.Length)
            {
                _reader.ReadBytes(buffer);
            }
            else
            {
                var buff = new byte[count];
                _reader.ReadBytes(buff);
                Array.Copy(buff, 0, buffer, offset, count);
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset == 0 && count == buffer.Length)
            {
                _writer.WriteBytes(buffer);
            }
            else
            {
                var buff = new byte[count];
                Array.Copy(buffer, offset, buff, 0, count);
                _writer.WriteBytes(buff);
            }
            _writer.StoreAsync().GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
