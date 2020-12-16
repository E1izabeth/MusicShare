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
    class LocalUtility : ILocalUtility
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
                                audioTrack = new AudioTrack(
                                    Android.Media.Stream.Music,
                                    sampleRateInHz,
                                    channelConfig,
                                    Android.Media.Encoding.Pcm16bit,
                                    AudioTrack.GetMinBufferSize(sampleRateInHz, channelConfig, Android.Media.Encoding.Pcm16bit) * 2,
                                    AudioTrackMode.Stream);

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

        const string BT_NAME = "test";
        static readonly Java.Util.UUID BT_UUID = Java.Util.UUID.FromString("46AC7CFF-6EC4-4529-8CBE-DC34673C7460");


        public async void ListenBt()
        {
            var listener = BluetoothAdapter.DefaultAdapter.ListenUsingRfcommWithServiceRecord(BT_NAME, BT_UUID);
            var sck = await listener.AcceptAsync();

            this.DoDataExchange(sck);
        }

        public async Task<BtDeviceEntry[]> DiscoverBt()
        {
            BluetoothAdapter.DefaultAdapter.StartDiscovery();

            var result = new Dictionary<string, BtDeviceEntry>();

            for (int i = 0; i < 50; i++)
            {
                foreach (var item in BluetoothAdapter.DefaultAdapter.BondedDevices)
                {
                    if (item.BondState == Bond.Bonded)
                    {
                        result[item.Address] = new BtDeviceEntry(item.Address, item.Name);
                    }
                }

                await Task.Delay(100);
            }

            BluetoothAdapter.DefaultAdapter.CancelDiscovery();

            return result.Values.ToArray();
        }

        public void ConnectBt(string addr)
        {
            var remoteDevice = BluetoothAdapter.DefaultAdapter.BondedDevices.First(d => d.Address == addr);
            var tmp = remoteDevice.CreateRfcommSocketToServiceRecord(BT_UUID);
            tmp.Connect();
            this.DoDataExchange(tmp);
        }

        private void DoDataExchange(BluetoothSocket sck)
        {
            _thread = new Thread(() => {
                var reader = new BinaryReader(sck.InputStream);
                while (sck.IsConnected)
                {
                    var len = reader.ReadInt32();
                    var text = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(len));
                    System.Diagnostics.Debug.Print(text);
                }
            });
            _thread.Start();

            _thread2 = new Thread(() => {
                var writer = new BinaryWriter(sck.OutputStream);
                for (int i = 0; i < 100; i++)
                {
                    var data = System.Text.Encoding.UTF8.GetBytes("btmsg: " + i + " - " + DateTime.Now);
                    writer.Write(data.Length);
                    writer.Write(data);
                    Thread.Sleep(50);
                }
            });
            _thread2.Start();
        }
    }
}