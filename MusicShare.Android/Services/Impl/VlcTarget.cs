//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;

//using LibVLCSharp.Shared;
//using MusicShare.Interaction.Standard;
//using MusicShare.Interaction.Standard.Common;
//using MusicShare.Interaction.Standard.Stream;

//namespace MusicShare.Droid.Services.Impl
//{
//    class VlcTarget : DisposableObject, IPlayerTarget
//    {
//        public event Action<Exception> OnError;

//        public VlcTarget()
//        {
//            // new StreamMediaInput(stream

//            using (var disposables = new DisposableList())
//            {
//                var libVlc = disposables.Add(new LibVLC(true));
//                libVlc.Log += (sender, ea) => System.Diagnostics.Debug.Print(ea.FormattedLog);

//                var media = disposables.Add(new Media(libVlc, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"), ":no-video"));
//                var mediaPlayer = new MediaPlayer(media);

//                mediaPlayer.SetAudioFormatCallback(AudioSetup, AudioCleanup);
//                mediaPlayer.SetAudioCallbacks(PlayAudio, PauseAudio, ResumeAudio, FlushAudio, DrainAudio);

//                mediaPlayer.Play();
//                mediaPlayer.Time = 20_000; // Seek the video 20 seconds
//                outputDevice.Play();



//                void PlayAudio(IntPtr data, IntPtr samples, uint count, long pts)
//                {
//                    int bytes = (int)count * 2; // (16 bit, 1 channel)
//                    var buffer = new byte[bytes];
//                    Marshal.Copy(samples, buffer, 0, bytes);

//                    waveProvider.AddSamples(buffer, 0, bytes);
//                    writer.Write(buffer, 0, bytes);
//                }

//                int AudioSetup(ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels)
//                {
//                    channels = (uint)waveFormat.Channels;
//                    rate = (uint)waveFormat.SampleRate;
//                    return 0;
//                }

//                void DrainAudio(IntPtr data)
//                {
//                    writer.Flush();
//                }

//                void FlushAudio(IntPtr data, long pts)
//                {
//                    writer.Flush();
//                    waveProvider.ClearBuffer();
//                }

//                void ResumeAudio(IntPtr data, long pts)
//                {
//                    outputDevice.Play();
//                }

//                void PauseAudio(IntPtr data, long pts)
//                {
//                    outputDevice.Pause();
//                }

//                void AudioCleanup(IntPtr opaque) { }


//            }
//        }

//        public void Start()
//        {
//            throw new NotImplementedException();
//        }

//        public void Write(RawData frame, bool isTail)
//        {
//            throw new NotImplementedException();
//        }

//        protected override void DisposeImpl()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}