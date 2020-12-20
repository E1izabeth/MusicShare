using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.ViewModels;
using MusicShare.Views;
using Xamarin.Forms;

namespace MusicShare.Droid.Util
{
    class LocalUtility 
    {
        readonly MainActivity _mainActivity;

        public LocalUtility(MainActivity mainActivity)
        {
            _mainActivity = mainActivity;
        }

        Thread _thread, _thread2;

        public void Play(string uri)
        {
            System.Diagnostics.Debug.Print(uri);

            var extractor = new MediaExtractor();

            if (File.Exists(uri))
            {
                extractor.SetDataSource(uri);
            }
            else
            {
                var fd = _mainActivity.ContentResolver.OpenFileDescriptor(Android.Net.Uri.Parse(uri), "r");
                extractor.SetDataSource(fd.FileDescriptor, 0, fd.StatSize);
            }

            extractor.SelectTrack(0);

            ////AudioManager
            //var formatToEncode = MediaFormat.CreateAudioFormat(MediaFormat.MimetypeAudioAac, 44100, 2);
            //var encoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAac);
            //encoder.SetCallback(createCallbackEncoder());
            //encoder.Configure(formatToEncode, null, null, MediaCodecConfigFlags.Encode);
            
            // codecList.FindEncoderForFormat(


            var trackFormat = extractor.GetTrackFormat(0);
            var decoder = MediaCodec.CreateDecoderByType(trackFormat.GetString(MediaFormat.KeyMime));
            decoder.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);

            _thread = new Thread(() => {
                decoder.Start();
                var decoderInputBuffers = decoder.GetInputBuffers();
                var decoderOutputBuffers = decoder.GetOutputBuffers();

                var inputIndex = decoder.DequeueInputBuffer(-1);
                var inputBuffer = decoderInputBuffers[inputIndex];
                var bufferInfo = new MediaCodec.BufferInfo();
                byte[] audioBuffer = null;
                AudioTrack audioTrack = null;

                var read = extractor.ReadSampleData(inputBuffer, 0);
                while (read > 0)
                {
                    decoder.QueueInputBuffer(inputIndex, 0, read, extractor.SampleTime,
                        extractor.SampleFlags == MediaExtractorSampleFlags.Sync ? MediaCodecBufferFlags.SyncFrame : MediaCodecBufferFlags.None);

                    extractor.Advance();

                    var outputIndex = decoder.DequeueOutputBuffer(bufferInfo, -1);
                    if (outputIndex == (int)MediaCodecInfoState.OutputFormatChanged)
                    {
                        trackFormat = decoder.OutputFormat;
                    }
                    else if (outputIndex >= 0)
                    {
                        if (bufferInfo.Size > 0)
                        {
                            var outputBuffer = decoderOutputBuffers[outputIndex];
                            if (audioBuffer == null || audioBuffer.Length < bufferInfo.Size)
                            {
                                audioBuffer = new byte[bufferInfo.Size];
                                System.Diagnostics.Debug.WriteLine("Allocated new audiobuffer: {0}", audioBuffer.Length);
                            }

                            outputBuffer.Rewind();
                            outputBuffer.Get(audioBuffer, 0, bufferInfo.Size);
                            decoder.ReleaseOutputBuffer(outputIndex, false);

                            if (audioTrack == null)
                            {
                                var sampleRateInHz = trackFormat.GetInteger(MediaFormat.KeySampleRate);
                                var channelCount = trackFormat.GetInteger(MediaFormat.KeyChannelCount);
                                var channelConfig = channelCount == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
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

                                audioTrack = new AudioTrack.Builder()
                                                           .SetTransferMode(AudioTrackMode.Stream)
                                                           .SetPerformanceMode(AudioTrackPerformanceMode.None)
                                                           .SetBufferSizeInBytes(bufferSizeInBytes)
                                                           .SetAudioFormat(audioFormat)
                                                           .SetAudioAttributes(audioAttributes)
                                                           .Build();
                                audioTrack.Play();
                            }

                            audioTrack.Write(audioBuffer, 0, bufferInfo.Size);
                        }
                    }

                    inputIndex = decoder.DequeueInputBuffer(-1);
                    inputBuffer = decoderInputBuffers[inputIndex];

                    read = extractor.ReadSampleData(inputBuffer, 0);
                }
            });

            _thread.Start();
        }

    }
}