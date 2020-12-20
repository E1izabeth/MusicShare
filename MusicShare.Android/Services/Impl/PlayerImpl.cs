using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MusicShare.Droid.Services.Impl
{
    class PlayerImpl : DisposableObject, IPlayer
    {
        public event Action<StreamDataPacket> OnAudioData;

        readonly Service _service;

        public BluetoothConnector BtConnector { get; }
        public NetworkConnector NetConnector { get; }
        public PlayerPlaylist Playlist { get; }

        IBluetoothConnector IPlayer.BtConnector { get { return this.BtConnector; } }
        INetworkConnector IPlayer.NetConnector { get { return this.NetConnector; } }
        IPlayerPlaylist IPlayer.Playlist { get { return this.Playlist; } }

        LocalAudioPayerTarget _localTarget;
        MediaStreamPlayerTarget _streamTarget;

        MediaFilePayerSource _fileSource;
        MediaStreamPlayerSource _streamSource;

        readonly WorkerThreadPool _threadPool = new WorkerThreadPool(3);

        public PlayerImpl(Service service)
        {
            _service = service;

            this.Playlist = new PlayerPlaylist(service.ApplicationContext);

            var name = Android.Provider.Settings.Secure.GetString(service.ContentResolver, "bluetooth_name");
            
            this.BtConnector = new BluetoothConnector(service);
            this.NetConnector = new NetworkConnector(service, 32332, name);
        }

        public bool IsPlaying { get; internal set; }

        public void Start()
        {
            _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, this.Playlist.Get(this.Playlist.ActiveTrackIndex).FilePathOrUri);
            _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);

            _fileSource.OnRawData += (f, t) => {
                _localTarget.Write(f, t);
                if (t)
                {
                    _fileSource.SafeDispose();
                    if (this.Playlist.TryAdvance())
                    {
                        _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, this.Playlist.Get(this.Playlist.ActiveTrackIndex).FilePathOrUri);
                        _fileSource.OnRawData += (f, t) => {
                            _localTarget.Write(f, t);
                        };
                    }
                }
            };

            _localTarget.Start();
            _fileSource.Start();
        }

        public void Pause()
        {
            _fileSource.SafeDispose();
            _localTarget.SafeDispose();
        }

        public void Stop()
        {
            try
            {
                _fileSource = PlayerSource.FromPathOrUri(_service.ApplicationContext.ContentResolver, this.Playlist.Get(0).FilePathOrUri);
                _streamTarget = PlayerTarget.ToMediaStream(0, _fileSource.SampleRateHz, _fileSource.IsMono);

                _fileSource.OnRawData += (f, t) => _streamTarget.Write(f, t);
                _streamTarget.OnData += (p) => {
                    if (p is StreamDataHeadPacket h)
                    {
                        _streamSource = PlayerSource.FromMediaStream(h);
                        _localTarget = PlayerTarget.ToLocalAudio(_fileSource.SampleRateHz, _fileSource.IsMono);
                        _streamSource.OnRawData += (f, t) => _localTarget.Write(f, t);

                        _threadPool.Schedule(() => {
                            _localTarget.Start();
                            _streamSource.Start();
                        });
                    }
                    else
                    {
                        _streamSource.PushStreamData(p);
                    }
                };

                _streamTarget.Start();
                _fileSource.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FAIL");
            }
        }

        public void Reset()
        {
        }

        public void SetVolume(float volume)
        {
            _localTarget.SetVolume(volume);
        }

        public void PlayNextTrack()
        {
            this.Playlist.TryAdvance();
        }

        public void PlayPrevTrack()
        {

        }

        protected override void DisposeImpl()
        {
            this.NetConnector.SafeDispose();
            this.BtConnector.SafeDispose();
            this.Playlist.SafeDispose();
        }
    }
}