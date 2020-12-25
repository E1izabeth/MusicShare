using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Nio;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MusicShare.Droid.Services.Impl
{

    interface IPlayerSource : IDisposable
    {
        int SampleRateHz { get; }
        bool IsMono { get; }

        event Action<RawData, TimeSpan, bool> OnRawData;
        event Action<Exception> OnError;

        void Start();
    }

    abstract class PlayerSource : DisposableObject, IPlayerSource
    {
        public event Action<RawData, TimeSpan, bool> OnRawData;
        public event Action<Exception> OnError;

        public int SampleRateHz { get; protected set; }
        public bool IsMono { get; protected set; }

        public PlayerSource()
        {
        }

        protected void RaizeOnRawDataEvent(RawData data, TimeSpan dueTime, bool isTail) { this.OnRawData?.Invoke(data, dueTime, isTail); }
        protected void RaizeOnErrorEvent(Exception ex) { this.OnError?.Invoke(ex); }

        public void Start()
        {
            this.StartImpl();
        }

        protected abstract void StartImpl();

        // ---------------------------------------------------------

        public static AsyncMediaStreamPlayerSource FromMediaStream(StreamDataHeadPacket head)
        {
            return new AsyncMediaStreamPlayerSource(head);
        }

        public static MediaFilePayerSource FromPathOrUri(ContentResolver contentResolver, string filePathOrUri)
        {
            if (File.Exists(filePathOrUri))
            {
                return new MediaFilePayerSource(filePathOrUri);
            }
            else
            {
                var uri = Android.Net.Uri.Parse(filePathOrUri);
                var fd = contentResolver.OpenFileDescriptor(uri, "r");
                return new MediaFilePayerSource(uri, fd);
            }
        }

        protected class DecoderCallback : MediaCodec.Callback
        {
            const string _tag = "PlayerSource.DecoderCallback";

            readonly PlayerSource _owner;
            readonly MediaExtractor _extractor;
            readonly MediaCodec _codec;

            volatile bool _isWorking = true, _isFinished = false, _isPaused = false;
            public bool IsWorking
            {
                get { return _isWorking; }
                set { _isWorking = value; }
            }

            public DecoderCallback(PlayerSource owner, MediaExtractor extractor, MediaCodec codec)
            {
                _owner = owner;
                _extractor = extractor;
                _codec = codec;
            }

            volatile int _currentBufferIndex;

            public void Proceed()
            {
                if (_isFinished || !this.IsWorking) // _isPaused
                    return;

                _isWorking = true;
                var index = _currentBufferIndex;
                var codec = _codec;
                var mExtractor = _extractor;

                ByteBuffer byteBuffer = codec.GetInputBuffer(index);
                // Log.i(TAG, "onInputBufferAvailable: byteBuffer b/f readSampleData (decoder): " + byteBuffer);
                if (byteBuffer != null)
                {
                    try
                    {
                        int size = mExtractor.ReadSampleData(byteBuffer, 0);
                        if (size > -1)
                        {
                            var decoderFlags = mExtractor.SampleFlags == MediaExtractorSampleFlags.Sync
                                                                       ? MediaCodecBufferFlags.SyncFrame
                                                                       : MediaCodecBufferFlags.None;

                            codec.QueueInputBuffer(index, 0, size, mExtractor.SampleTime, decoderFlags);

                            mExtractor.Advance();
                        }
                        else
                        {
                            codec.QueueInputBuffer(index, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                            codec.SignalEndOfInputStream();
                            _isFinished = true;
                        }
                        // Log.i(TAG, "onInputBufferAvailable (decoder): SUCCESS");
                    }
                    catch (Exception e)
                    {
                        if (!_owner.IsDisposed)
                        {
                            Log.e(_tag, "EXCEPTION (decoder)!\nonInputBufferAvailable (decoder): ", e);
                            //throw e;
                        }
                    }
                }
                else
                {
                    Log.e(_tag, "onInputBufferAvailable = null");
                }
            }

            public override void OnInputBufferAvailable(MediaCodec codec, int index)
            {
                _currentBufferIndex = index;
                if (this.IsWorking)
                {
                    this.Proceed();
                }
                else
                {
                    _isPaused = true;
                }
            }

            byte[] _audioBuffer = null;

            public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
            {
                ByteBuffer byteBuffer = codec.GetOutputBuffer(index);
                // Log.i(TAG, "onOutputBufferAvailable: byteBuffer with data (decoder): " + byteBuffer);

                var isTail = info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream);
                if (byteBuffer != null)
                {
                    if (_audioBuffer == null || _audioBuffer.Length < info.Size)
                        _audioBuffer = new byte[info.Size];

                    Marshal.Copy(byteBuffer.GetDirectBufferAddress(), _audioBuffer, 0, info.Size);

                    var frame = new RawData(_audioBuffer, 0, info.Size);
                    _owner.RaizeOnRawDataEvent(frame, TimeSpan.FromTicks(info.PresentationTimeUs * 10), isTail);

                    #region
                    // ByteBuffer buffer2 = ByteBuffer.Allocate(info.Size);
                    // Log.i(TAG, "onOutputBufferAvailable: allocated byteBuffer (decoder): " + buffer2);
                    //buffer2.Put(byteBuffer);
                    //MediaCodec.BufferInfo info2 = new MediaCodec.BufferInfo();
                    //info2.flags = info.flags;
                    //info2.size = info.size;
                    //info2.presentationTimeUs = info.presentationTimeUs;
                    //info2.offset = info.offset;
                    //Log.i(TAG, "onOutputBufferAvailable (decoder): added in queue: %s\n%s %s %s %s", buffer2, info2.offset, info2.size, info2.presentationTimeUs, info2.flags);
                    #endregion

                    codec.ReleaseOutputBuffer(index, false);
                }
                else
                {
                    // Log.e(TAG, "onOutputBufferAvailable = null");
                    if (info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
                    {
                        var frame = new RawData(_audioBuffer, 0, 0);
                        _owner.RaizeOnRawDataEvent(frame, TimeSpan.MaxValue, isTail);
                    }
                }
            }

            public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
            {
                // should never happen for a typical audio track?? 
                // Log.i(TAG, "onOutputFormatChanged (decoder): OLD=%s NEW=%s", codec.InputFormat, format);
                //mEncoder.start();
            }

            public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
            {
                if (!_owner.IsDisposed)
                {
                    _owner.RaizeOnErrorEvent(new PlayerException(e));
                }
            }
        }
    }

    class MediaFilePayerSource : PlayerSource
    {
        public string FilePath { get; private set; }
        public Android.Net.Uri FileUri { get; private set; }

        readonly ParcelFileDescriptor _fd;
        readonly MediaExtractor _extractor = new MediaExtractor();
        readonly MediaCodec _decoder;
        DecoderCallback _callback;

        public MediaFilePayerSource(Android.Net.Uri fileUri, ParcelFileDescriptor fd)
        {
            this.FilePath = fileUri.ToString();
            this.FileUri = fileUri;

            _fd = fd;
            _extractor.SetDataSource(fd.FileDescriptor, 0, fd.StatSize);
            _decoder = this.Initialize();
        }

        public MediaFilePayerSource(string filePathOrUri)
        {
            this.FilePath = filePathOrUri;
            this.FileUri = null;

            _fd = null;
            _extractor.SetDataSource(filePathOrUri);
            _decoder = this.Initialize();
        }

        private MediaCodec Initialize()
        {
            _extractor.SelectTrack(0);

            var trackFormat = _extractor.GetTrackFormat(0);
            this.SampleRateHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
            this.IsMono = trackFormat.GetInteger(MediaFormat.KeyChannelCount) == 1;

            var decoder = MediaCodec.CreateDecoderByType(trackFormat.GetString(MediaFormat.KeyMime));
            decoder.SetCallback(_callback = new DecoderCallback(this, _extractor, decoder));
            decoder.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);

            return decoder;
        }

        protected override void StartImpl()
        {
            _decoder.Start();
        }

        protected override void DisposeImpl()
        {
            _decoder.SafeDispose();
            _extractor.SafeDispose();

            if (_fd != null)
                _fd.SafeDispose();
        }

        public void Pause()
        {
            _callback.IsWorking = false;
        }

        public void Resume()
        {
            _callback.Proceed();
        }
    }

    //class MediaStreamPlayerSource : PlayerSource, IDataPacketHandler
    //{
    //    class RamDataSource : MediaDataSource
    //    {
    //        volatile bool _isFinished = false;
    //        long _needlePosition;
    //        readonly object _lock = new object();
    //        readonly long _totalSize;
    //        readonly MemoryStream _ram = new MemoryStream();

    //        public RamDataSource(long totalSize)
    //        {
    //            _totalSize = totalSize;
    //        }

    //        public override long Size { get { return _totalSize; } }

    //        public override int ReadAt(long position, byte[] buffer, int offset, int size)
    //        {
    //            Log.TraceMethod("enter");
    //            try
    //            {
    //                lock (_lock)
    //                {
    //                    if (!_isFinished && _ram.Length < position)
    //                    {
    //                        Interlocked.Exchange(ref _needlePosition, position);
    //                        Log.TraceMethod($"waiting for {size} bytes to be at {position}");
    //                        Monitor.Wait(_lock);
    //                    }

    //                    if (_ram.Length < position)
    //                    {
    //                        Log.TraceMethod($"data is over");
    //                        return 0;
    //                    }

    //                    _ram.Position = position;
    //                    Log.TraceMethod($"reading {size} bytes at {position}");
    //                    return _ram.Read(buffer, offset, size);
    //                }
    //            }
    //            finally
    //            {
    //                Log.TraceMethod("exit");
    //            }
    //        }

    //        public override void Close()
    //        {
    //        }

    //        public void PushData(RawData dataFrame, bool finish = false)
    //        {
    //            Log.TraceMethod("enter");
    //            lock (_lock)
    //            {
    //                _ram.Position = _ram.Length;
    //                _ram.Write(dataFrame.Data, dataFrame.Offset, dataFrame.Size);
    //                _isFinished = finish;
    //                Log.TraceMethod($"{dataFrame.Size} bytes written at {dataFrame.Offset}");


    //                if (_ram.Length > Interlocked.Read(ref _needlePosition) || finish)
    //                {
    //                    Log.TraceMethod("pulsing");
    //                    Monitor.Pulse(_lock);
    //                }
    //            }
    //            Log.TraceMethod("exit");
    //        }
    //    }

    //    readonly RamDataSource _ramDataSource;
    //    readonly MediaExtractor _extractor = new MediaExtractor();
    //    readonly StreamDataHeadPacket _streamHead;
    //    MediaCodec _decoder;

    //    public MediaStreamPlayerSource(StreamDataHeadPacket streamHeadPacket)
    //    {
    //        _streamHead = streamHeadPacket;

    //        _ramDataSource = new RamDataSource(streamHeadPacket.TotalSize);
    //        _ramDataSource.PushData(streamHeadPacket.DataFrame);
    //    }

    //    public void PushStreamData(StreamDataPacket packet)
    //    {
    //        packet.HandleWith(this);
    //    }

    //    protected override void StartImpl()
    //    {
    //        Log.TraceMethod("enter");
    //        _extractor.SetDataSource(_ramDataSource);
    //        _extractor.SelectTrack(0);

    //        var trackFormat = _extractor.GetTrackFormat(0);
    //        this.SampleRateHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
    //        this.IsMono = trackFormat.GetInteger(MediaFormat.KeyChannelCount) == 1;

    //        Log.TraceMethod("preparing decoder");
    //        _decoder = MediaCodec.CreateDecoderByType(trackFormat.GetString(MediaFormat.KeyMime));
    //        _decoder.SetCallback(new DecoderCallback(this, _extractor));
    //        _decoder.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);
    //        _decoder.Start();

    //        Log.TraceMethod("ready");
    //    }

    //    protected override void DisposeImpl()
    //    {
    //        _decoder.SafeDispose();
    //        _extractor.SafeDispose();
    //    }

    //    void IDataPacketHandler.HandleStreamHead(StreamDataHeadPacket packet)
    //    {
    //        throw new InvalidOperationException("MediaStreamPlayerSource recreation needed");
    //    }

    //    void IDataPacketHandler.HandleStreamBody(StreamDataBodyPacket packet)
    //    {
    //        _ramDataSource.PushData(packet.DataFrame);
    //    }

    //    void IDataPacketHandler.HandleStreamTail(StreamDataTailPacket packet)
    //    {
    //        _ramDataSource.PushData(packet.DataFrame, true);
    //    }
    //}

    class AsyncMediaStreamPlayerSource : PlayerSource, IDataPacketHandler
    {
        class RamDataSource : MediaDataSource
        {
            volatile bool _isFinished = false;
            long _needlePosition, _totalSize;
            readonly object _lock = new object();
            readonly MemoryStream _ram = new MemoryStream();

            public RamDataSource(long totalSize)
            {
                _totalSize = totalSize;
            }

            public override long Size { get { return _ram.Length; } }

            public override int ReadAt(long position, byte[] buffer, int offset, int size)
            {
                Log.TraceMethod("enter");
                try
                {
                    lock (_lock)
                    {
                        if (!_isFinished && _ram.Length <= position && _totalSize > position)
                        {
                            Volatile.Write(ref _needlePosition, position);
                            Log.TraceMethod($"waiting for {size} bytes to be at {position}");
                            Monitor.Wait(_lock);
                        }

                        if (_totalSize <= position && _isFinished)
                        {
                            Log.TraceMethod($"data is over");
                            return -1;
                        }

                        _ram.Position = position;
                        Log.TraceMethod($"reading {size} bytes at {position}");
                        return _ram.Read(buffer, offset, size);
                    }
                }
                finally
                {
                    Log.TraceMethod("exit");
                }
            }

            public override void Close()
            {
            }

            public void PushData(RawData dataFrame, bool finish = false)
            {
                Log.TraceMethod("enter");
                lock (_lock)
                {
                    var pos = _ram.Length;
                    _ram.Position = pos;
                    _ram.Write(dataFrame.Data, dataFrame.Offset, dataFrame.Size);
                    _isFinished = finish;
                    Log.TraceMethod($"{dataFrame.Size} bytes written at {pos}");

                    if (finish)
                    {
                        _totalSize = _ram.Length;
                    }

                    if (_ram.Length > Volatile.Read(ref _needlePosition) || finish)
                    {
                        Log.TraceMethod("pulsing");
                        Monitor.Pulse(_lock);
                    }
                }
                Log.TraceMethod("exit");
            }
        }

        readonly RamDataSource _ramDataSource;
        readonly MediaExtractor _extractor = new MediaExtractor();
        readonly StreamDataHeadPacket _streamHead;

        readonly Thread _thread;

        public AsyncMediaStreamPlayerSource(StreamDataHeadPacket streamHeadPacket)
        {
            _streamHead = streamHeadPacket;

            _ramDataSource = new RamDataSource(streamHeadPacket.TotalSize);
            _ramDataSource.PushData(streamHeadPacket.DataFrame);

            _thread = new Thread(this.DecoderThreadProc) {
                IsBackground = true
            };
        }

        public void PushStreamData(StreamDataPacket packet)
        {
            packet.HandleWith(this);
        }

        protected override void StartImpl()
        {
            _thread.Start();
        }

        private void DecoderThreadProc()
        {
            Log.TraceMethod("enter");
            _extractor.SetDataSource(_ramDataSource);
            _extractor.SelectTrack(0);

            var trackFormat = _extractor.GetTrackFormat(0);
            this.SampleRateHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
            this.IsMono = trackFormat.GetInteger(MediaFormat.KeyChannelCount) == 1;

            Log.TraceMethod("preparing decoder");
            var decoder = MediaCodec.CreateDecoderByType(trackFormat.GetString(MediaFormat.KeyMime));
            decoder.SetCallback(new DecoderCallback(this, _extractor, decoder));
            decoder.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);
            decoder.Start();

            Log.TraceMethod("ready");


            var inputIndex = decoder.DequeueInputBuffer(-1);
            var inputBuffer = decoder.GetInputBuffer(inputIndex);
            var bufferInfo = new MediaCodec.BufferInfo();
            byte[] audioBuffer = null;

            var read = _extractor.ReadSampleData(inputBuffer, 0);
            while (read > 0)
            {
                decoder.QueueInputBuffer(inputIndex, 0, read, _extractor.SampleTime,
                    _extractor.SampleFlags == MediaExtractorSampleFlags.Sync ? MediaCodecBufferFlags.SyncFrame : MediaCodecBufferFlags.None);

                _extractor.Advance();

                var outputIndex = decoder.DequeueOutputBuffer(bufferInfo, -1);
                if (outputIndex == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    trackFormat = decoder.OutputFormat;
                }
                else if (outputIndex >= 0)
                {
                    if (bufferInfo.Size > 0)
                    {
                        var outputBuffer = decoder.GetOutputBuffer(outputIndex);
                        if (audioBuffer == null || audioBuffer.Length < bufferInfo.Size)
                        {
                            audioBuffer = new byte[bufferInfo.Size];
                            System.Diagnostics.Debug.WriteLine("Allocated new audiobuffer: {0}", audioBuffer.Length);
                        }

                        outputBuffer.Rewind();
                        Marshal.Copy(outputBuffer.GetDirectBufferAddress(), audioBuffer, 0, bufferInfo.Size);

                        var isTail = bufferInfo.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream);
                        var frame = new RawData(audioBuffer, 0, bufferInfo.Size);
                        this.RaizeOnRawDataEvent(frame, TimeSpan.FromTicks(bufferInfo.PresentationTimeUs * 10), isTail);

                        decoder.ReleaseOutputBuffer(outputIndex, false);
                    }
                }

                inputIndex = decoder.DequeueInputBuffer(-1);
                inputBuffer = decoder.GetInputBuffer(inputIndex);

                read = _extractor.ReadSampleData(inputBuffer, 0);
            }
        }

        protected override void DisposeImpl()
        {
            //_decoder.SafeDispose();
            _extractor.SafeDispose();
        }

        void IDataPacketHandler.HandleStreamHead(StreamDataHeadPacket packet)
        {
            throw new InvalidOperationException("MediaStreamPlayerSource recreation needed");
        }

        void IDataPacketHandler.HandleStreamBody(StreamDataBodyPacket packet)
        {
            _ramDataSource.PushData(packet.DataFrame);
        }

        void IDataPacketHandler.HandleStreamTail(StreamDataTailPacket packet)
        {
            _ramDataSource.PushData(packet.DataFrame, true);
        }
    }
}