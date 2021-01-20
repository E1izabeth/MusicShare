using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard.Common
{
    using System.IO;

    public class BidirectionalStream : Stream
    {
        Stream _source;
        Stream _destination;

        public BidirectionalStream(Stream source, Stream destination)
        {
            _source = source;
            _destination = destination;
        }


        public override bool CanRead { get { return _source.CanRead; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return _destination.CanWrite; } }

        public override void Flush()
        {
            _destination.Flush();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _destination.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _source.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _destination.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            try { _destination.Close(); }
            catch (Exception ex) { }
            try { _source.Close(); }
            catch (Exception ex) { }
            base.Close();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _source.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _destination.EndWrite(asyncResult);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _destination.Dispose(); }
                catch (Exception ex) { }
                try { _source.Dispose(); }
                catch (Exception ex) { }
            }

            base.Dispose(disposing);
        }

        public override int ReadByte()
        {
            return _source.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            _destination.WriteByte(value);
        }

        public override int ReadTimeout
        {
            get { return _source.ReadTimeout; }
            set { _source.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return _destination.WriteTimeout; }
            set { _destination.WriteTimeout = value; }
        }
    }
}
