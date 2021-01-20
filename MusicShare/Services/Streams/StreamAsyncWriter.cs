using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MusicShare.Interaction.Standard.Stream;

namespace MusicShare.Services.Streams
{
    public class StreamAsyncWriter
    {
        public Stream Stream { get; }

        private readonly object _lock = new object();
        private readonly Queue<RawData> _queue = new Queue<RawData>();
        private readonly AsyncCallback _writeProc;
        private volatile bool _sending = false;
        private volatile bool _broken = false;

        public StreamAsyncWriter(Stream stream)
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
            try
            {
                this.Stream.EndWrite(ar);
                this.Stream.Flush();
            }
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
}
