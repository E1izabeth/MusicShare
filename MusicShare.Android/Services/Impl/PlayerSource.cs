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

        event Action<RawData, bool> OnRawData;
        event Action<Exception> OnError;

        void Start();
    }

    abstract class PlayerSource : DisposableObject, IPlayerSource
    {
        public event Action<RawData, bool> OnRawData;
        public event Action<Exception> OnError;

        public int SampleRateHz { get; protected set; }
        public bool IsMono { get; protected set; }

        public PlayerSource()
        {
        }

        protected void RaizeOnRawDataEvent(RawData data, bool isTail) { this.OnRawData?.Invoke(data, isTail); }
        protected void RaizeOnErrorEvent(Exception ex) { this.OnError?.Invoke(ex); }

        public void Start()
        {
            this.StartImpl();
        }

        protected abstract void StartImpl();

        // ---------------------------------------------------------

        public static MediaStreamPlayerSource FromMediaStream(StreamDataHeadPacket head)
        {
            return new MediaStreamPlayerSource(head);
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

            public DecoderCallback(PlayerSource owner, MediaExtractor extractor)
            {
                _owner = owner;
                _extractor = extractor;
            }

            public override void OnInputBufferAvailable(MediaCodec codec, int index)
            {
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
                            codec.QueueInputBuffer(index, 0, size, mExtractor.SampleTime, MediaCodecBufferFlags.EndOfStream);
                        }
                        // Log.i(TAG, "onInputBufferAvailable (decoder): SUCCESS");
                    }
                    catch (Exception e)
                    {
                        if (!_owner.IsDisposed)
                        {
                            Log.e(_tag, "EXCEPTION (decoder)!\nonInputBufferAvailable (decoder): ", e);
                            throw e;
                        }
                    }
                }
                else
                {
                    Log.e(_tag, "onInputBufferAvailable = null");
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
                    _owner.RaizeOnRawDataEvent(frame, isTail);

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
                        _owner.RaizeOnRawDataEvent(frame, isTail);
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
            decoder.SetCallback(new DecoderCallback(this, _extractor));
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
    }

    class MediaStreamPlayerSource : PlayerSource, IDataPacketHandler
    {
        class RamDataSource : MediaDataSource
        {
            volatile bool _isFinished = false;
            long _needlePosition;
            readonly object _lock = new object();
            readonly long _totalSize;
            readonly MemoryStream _ram = new MemoryStream();

            public RamDataSource(long totalSize)
            {
                _totalSize = totalSize;
            }

            public override long Size { get { return _totalSize; } }

            public override int ReadAt(long position, byte[] buffer, int offset, int size)
            {
                lock (_lock)
                {
                    if (!_isFinished && _ram.Length < position)
                    {
                        Interlocked.Exchange(ref _needlePosition, position);
                        Monitor.Wait(_lock);
                    }

                    if (_ram.Length < position)
                        return 0;

                    _ram.Position = position;
                    return _ram.Read(buffer, offset, size);
                }
            }

            public override void Close()
            {
            }

            public void PushData(RawData dataFrame, bool finish = false)
            {
                lock (_lock)
                {
                    _ram.Position = _ram.Length;
                    _ram.Write(dataFrame.Data, dataFrame.Offset, dataFrame.Size);
                    _isFinished = finish;

                    if (_ram.Length > Interlocked.Read(ref _needlePosition) || finish)
                        Monitor.Pulse(_lock);
                }
            }
        }

        readonly RamDataSource _ramDataSource;
        readonly MediaExtractor _extractor = new MediaExtractor();
        readonly StreamDataHeadPacket _streamHead;
        MediaCodec _decoder;

        public MediaStreamPlayerSource(StreamDataHeadPacket streamHeadPacket)
        {
            _streamHead = streamHeadPacket;

            _ramDataSource = new RamDataSource(streamHeadPacket.TotalSize);
            _ramDataSource.PushData(streamHeadPacket.DataFrame);
        }

        public void PushStreamData(StreamDataPacket packet)
        {
            packet.HandleWith(this);
        }

        protected override void StartImpl()
        {
            _extractor.SetDataSource(_ramDataSource);
            _extractor.SelectTrack(0);

            var trackFormat = _extractor.GetTrackFormat(0);
            this.SampleRateHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
            this.IsMono = trackFormat.GetInteger(MediaFormat.KeyChannelCount) == 1;

            _decoder = MediaCodec.CreateDecoderByType(trackFormat.GetString(MediaFormat.KeyMime));
            _decoder.SetCallback(new DecoderCallback(this, _extractor));
            _decoder.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);
            _decoder.Start();
        }

        protected override void DisposeImpl()
        {
            _decoder.SafeDispose();
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