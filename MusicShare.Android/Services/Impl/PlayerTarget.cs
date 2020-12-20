﻿using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
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
    interface IPlayerTarget : IDisposable
    {
        event Action<Exception> OnError;

        void Start();
        void Write(RawData frame, bool isTail);
    }

    abstract class PlayerTarget : DisposableObject, IPlayerTarget
    {
        public event Action<Exception> OnError;

        public PlayerTarget()
        {

        }

        protected void RaizeOnErrorEvent(Exception ex) { this.OnError?.Invoke(ex); }

        public void Start()
        {
            this.StartImpl();
        }

        protected abstract void StartImpl();

        public void Write(RawData frame, bool isTail)
        {
            this.WriteImpl(frame, isTail);
        }

        protected abstract void WriteImpl(RawData frame, bool isTail);

        // ---------------------------------------------------------

        public static MediaStreamPlayerTarget ToMediaStream(int streamId, int sampleRate, bool mono)
        {
            return new MediaStreamPlayerTarget(streamId, sampleRate, mono);
        }

        public static LocalAudioPayerTarget ToLocalAudio(int sampleRateInHz, bool mono)
        {
            return new LocalAudioPayerTarget(sampleRateInHz, mono);
        }
    }

    class LocalAudioPayerTarget : PlayerTarget
    {
        readonly AudioTrack _audioTrack;

        public LocalAudioPayerTarget(int sampleRateInHz, bool mono)
        {
            // var sampleRateInHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
            // var channelCount = trackFormat.GetInteger(MediaFormat.KeyChannelCount);
            // var channelConfig = channelCount == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
            var channelConfig = mono ? ChannelOut.Mono : ChannelOut.Stereo;
            var bufferSizeInBytes = AudioTrack.GetMinBufferSize(sampleRateInHz, channelConfig, Android.Media.Encoding.Pcm16bit) * 2;

            var audioFormat = new AudioFormat.Builder()
                                             .SetEncoding(Android.Media.Encoding.Pcm16bit)
                                             .SetSampleRate(sampleRateInHz)
                                             .SetChannelMask(channelConfig)
                                             .Build();

            var audioAttributes = new AudioAttributes.Builder()
                                                     .SetContentType(AudioContentType.Music)
                                                     .SetLegacyStreamType(Android.Media.Stream.Music)
                                                     .Build();

            _audioTrack = new AudioTrack.Builder()
                                        .SetTransferMode(AudioTrackMode.Stream)
                                        .SetPerformanceMode(AudioTrackPerformanceMode.None)
                                        .SetBufferSizeInBytes(bufferSizeInBytes)
                                        .SetAudioFormat(audioFormat)
                                        .SetAudioAttributes(audioAttributes)
                                        .Build();
        }

        protected override void StartImpl()
        {
            _audioTrack.Play();
        }

        protected override void WriteImpl(RawData frame, bool isTail)
        {
            if (!this.IsDisposed)
            {
                var has = 0;
                while (has < frame.Size)
                {
                    var got = _audioTrack.Write(frame.Data, frame.Offset + has, frame.Size - has);
                    if (got < 0)
                        throw new InvalidOperationException("AudioTrack is invalid");

                    has += got;
                }
            }
        }

        protected override void DisposeImpl()
        {
            _audioTrack.SafeDispose();
        }

        public void SetVolume(float volume)
        {
            _audioTrack.SetVolume(volume);
        }
    }

    class MediaStreamPlayerTarget : PlayerTarget
    {
        public event Action<StreamDataPacket> OnData;

        private void RaizeOnDataEvent(StreamDataPacket packet) { this.OnData?.Invoke(packet); }

        readonly MediaFormat _mediaFormat;
        readonly MediaCodec _encoder;
        readonly int _sampleRate;
        readonly int _channelsCount;

        readonly FileInfo _file;
        readonly FileStream _fileStream;
        readonly FileOutputStream _fileOutputStream;
        readonly MediaMuxer _muxer;
        readonly int _audioTrackIndex;

        public int StreamId { get; private set; }

        readonly StreamDataHeadPacket _streamHead;

        public MediaStreamPlayerTarget(int streamId, int sampleRate, bool mono)
        {
            this.StreamId = streamId;

            _sampleRate = sampleRate;
            _channelsCount = mono ? 1 : 2;

            _streamHead = new StreamDataHeadPacket() {
                DataFrame = new RawData(new byte[0], 0, 0),
                StreamId = streamId,
            };

            MediaFormat wantedMediaFormat = MediaFormat.CreateAudioFormat(MediaFormat.MimetypeAudioAac, sampleRate, _channelsCount);
            wantedMediaFormat.SetInteger(MediaFormat.KeyBitRate, 64 * 1024);
            wantedMediaFormat.SetInteger(MediaFormat.KeyAacProfile, (int)MediaCodecProfileType.Aacobjecthe);
            
            var encoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAac);
            encoder.SetCallback(new EncoderCallback(this));
            encoder.Configure(wantedMediaFormat, null, null, MediaCodecConfigFlags.Encode);

            _mediaFormat = wantedMediaFormat;
            _encoder = encoder;

            //Java.IO.File.CreateTempFile("MediaStreamPlayerTarget", null, context.getCacheDir())
            _file = new FileInfo(Path.GetTempFileName());
            _fileStream = _file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            _fileOutputStream = new FileOutputStream(_file.FullName);
            _muxer = new MediaMuxer(_fileOutputStream.FD, MuxerOutputType.Mpeg4);
            _audioTrackIndex = _muxer.AddTrack(wantedMediaFormat);
        }

        protected override void StartImpl()
        {
            this.RaizeOnDataEvent(_streamHead);
            _encoder.Start();
            _muxer.Start();
        }

        readonly object _rawDataLock = new object();
        readonly ByteQueue _rawData = new ByteQueue();

        protected override void WriteImpl(RawData frame, bool isTail)
        {
            if (!this.IsDisposed)
            {
                lock (_rawDataLock)
                {
                    _rawData.Enqueue(frame.Data, frame.Offset, frame.Size);
                    Monitor.Pulse(_rawDataLock);
                }
            }
        }

        long _position = 0;
        byte[] _audioBuffer = null;

        private bool DrainMuxedData()
        {
            _fileOutputStream.Flush();

            var pos = Interlocked.Read(ref _position);
            var len = _file.Length;
            if (pos < len)
            {
                var step = (int)(len - pos);
                if (_audioBuffer == null || step > _audioBuffer.Length)
                    _audioBuffer = new byte[step];

                _fileStream.Position = pos;
                if (_fileStream.TryRead(_audioBuffer, 0, step))
                {
                    this.OnData(new StreamDataBodyPacket() {
                        DataFrame = new RawData(_audioBuffer, 0, step),
                        StreamId = this.StreamId
                    });
                }
                else
                {
                    this.FinishData();
                    return false;
                }
            }

            return true;
        }

        private void DrainMuxedDataEnd()
        {
            if (this.DrainMuxedData())
                this.FinishData();
        }

        private void FinishData()
        {
            this.OnData(new StreamDataTailPacket() {
                DataFrame = new RawData(new byte[0], 0, 0),
                StreamId = this.StreamId
            });
        }

        protected override void DisposeImpl()
        {
            _muxer.SafeDispose();
            _fileOutputStream.SafeDispose();
            _fileStream.SafeDispose();
            _encoder.SafeDispose();

            try { _file.Delete(); }
            catch
            {
                Thread.Sleep(100);
                _file.Delete();
            }
        }

        class EncoderCallback : MediaCodec.Callback
        {
            const string _tag = "MediaStreamPlayerTarget.EncoderCallback";

            readonly MediaStreamPlayerTarget _owner;

            public EncoderCallback(MediaStreamPlayerTarget owner)
            {
                _owner = owner;
            }

            long _currDuration = 0;

            public override void OnInputBufferAvailable(MediaCodec codec, int index)
            {
                var muxer = _owner._muxer;

                ByteBuffer byteBuffer = codec.GetInputBuffer(index);
                // Log.i(TAG, "onInputBufferAvailable: byteBuffer b/f readSampleData (decoder): " + byteBuffer);
                if (byteBuffer != null)
                {
                    try
                    {
                        int size;
                        lock (_owner._rawDataLock)
                        {
                            while (_owner._rawData.Length <= 0)
                                Monitor.Wait(_owner._rawDataLock);

                            byteBuffer.Rewind();
                            size = _owner._rawData.DequeueRaw(byteBuffer.GetDirectBufferAddress(), byteBuffer.Capacity());
                        }


                        if (size > -1)
                        {
                            codec.QueueInputBuffer(index, 0, size, _currDuration, MediaCodecBufferFlags.None);
                        }
                        else
                        {
                            codec.QueueInputBuffer(index, 0, size, _currDuration, MediaCodecBufferFlags.EndOfStream);
                        }

                        _currDuration += (size / _owner._channelsCount) * 1000000 / _owner._sampleRate;
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

            public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
            {
                ByteBuffer byteBuffer = codec.GetOutputBuffer(index);
                // Log.i(TAG, "onOutputBufferAvailable: byteBuffer with data (decoder): " + byteBuffer);

                var isTail = info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream);

                if (byteBuffer != null)
                {
                    _owner._muxer.WriteSampleData(_owner._audioTrackIndex, byteBuffer, info);

                    if (info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
                        _owner.DrainMuxedDataEnd();
                    else
                        _owner.DrainMuxedData();

                    codec.ReleaseOutputBuffer(index, false);
                }
                else
                {
                    if (info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
                        _owner.DrainMuxedDataEnd();
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
}