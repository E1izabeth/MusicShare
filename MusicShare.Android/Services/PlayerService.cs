using Android.App;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Droid.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicShare.Droid.Services
{

    [Service(Name = PlayerService.Name)]
    [IntentFilter(new[] { ActionTerminate })]
    public class PlayerService : IntentService, AudioManager.IOnAudioFocusChangeListener, IPlayerService
    {
        private static object _instanceLock = new object();

        public static event Action<IPlayerService> OnServiceReady;
        public static PlayerService Instance { get; private set; }

        private static void SetInstance(PlayerService service)
        {
            lock (_instanceLock)
            {
                Instance = service;
                ServiceContext.SetInstance(service);
                OnServiceReady?.Invoke(service);
                OnServiceReady = null;
            }
        }

        public static void GetLazyService(Action<IPlayerService> action)
        {
            lock (_instanceLock)
            {
                if (Instance == null)
                {
                    OnServiceReady += action;
                    return;
                }
            }

            action(Instance);
        }

        class PlayerServiceIntentReceiver : BroadcastReceiver
        {
            readonly PlayerService _owner;

            public PlayerServiceIntentReceiver(PlayerService owner)
            {
                _owner = owner;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                switch (intent.Action)
                {
                    case AudioManager.ActionAudioBecomingNoisy:
                        {
                            _owner._player.SetVolume(0.1f);
                        }
                        break;
                    default: // do nothing
                        break;
                }
            }
        }

        public const string ActionTerminate = "com.companyname.MusicShare.Droid.Services.PlayerService.Terminate";

        public const string Name = "com.companyname.MusicShare.Droid.Services.PlayerService";
        private const int _notificationId = 1;

        private PlayerServiceIntentReceiver _intentReceiver;

        private PlayerImpl _player;
        private AudioManager _audioManager;
        private WifiManager _wifiManager;
        private WifiManager.WifiLock _wifiLock;
        private bool _paused;

        private readonly PlayerServiceBinder _binder = null;

        IPlayer IPlayerService.Player { get { return _player; } }

        public PlayerService()
        {
            _binder = new PlayerServiceBinder(this);
        }

        /// <summary>
        /// On create simply detect some of our managers
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();

            SetInstance(this);
        }

        /// <summary>
        /// Don't do anything on bind
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        public override IBinder OnBind(Intent intent)
        {
            return _binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }

        private bool _initializing = true;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (_initializing)
            {
                _initializing = false;

                this.StartForeground();


                _player = new PlayerImpl(this);

                //Tell our player to sream music
                //player.SetAudioStreamType(Stream.Music);

                //Wake mode will be partial to keep the CPU still running under lock screen
                //player.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);

                _audioManager = (AudioManager)this.GetSystemService(AudioService);
                _wifiManager = (WifiManager)this.GetSystemService(WifiService);

                _intentReceiver = new PlayerServiceIntentReceiver(this);

                IntentFilter filter = new IntentFilter();
                filter.AddAction(AudioManager.ActionAudioBecomingNoisy);
                this.RegisterReceiver(_intentReceiver, filter);

            }

            return StartCommandResult.Sticky;
        }

        protected override void OnHandleIntent(Intent intent)
        {

        }

        void IPlayerService.Terminate()
        {
            var intent = new Intent(this, typeof(PlayerService));
            this.StopService(intent);
        }

        //private void Play()
        //{
        //    _player.Start();

        //    var req = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
        //                        .SetOnAudioFocusChangeListener(this)
        //                        .Build();

        //    _audioManager.RequestAudioFocus(req);

        //    this.AquireWifiLock();
        //}

        //private void Pause()
        //{
        //    if (_player == null)
        //        return;

        //    if (_player.IsPlaying)
        //        _player.Pause();

        //    this.StopForeground(true);
        //    _paused = true;
        //}

        //private void Stop()
        //{
        //    if (_player == null)
        //        return;

        //    if (_player.IsPlaying)
        //        _player.Stop();

        //    _player.Reset();
        //    _paused = false;
        //    this.StopForeground(true);
        //    this.ReleaseWifiLock();
        //}

        #region env utils

        /// <summary>
        /// When we start on the foreground we will present a notification to the user
        /// When they press the notification it will take them to the main page so they can control the music
        /// </summary>
        private void StartForeground()
        {
            this.StartForeground(_notificationId, this.BuildNotification());
        }

        private void UpdateNotification(string contentText = "...")
        {
            NotificationManager notificationManager = (NotificationManager)this.GetSystemService(Context.NotificationService);
            notificationManager.Notify(_notificationId, this.BuildNotification(contentText));
        }

        private Notification BuildNotification(string contentText = "...")
        {
            var pendingIntent = PendingIntent.GetActivity(
                this.ApplicationContext, 0, new Intent(this.ApplicationContext, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent
            );

            var channelId = Build.VERSION.SdkInt >= BuildVersionCodes.O ? this.CreateNotificationChannel(Name, "MusicShare Player Service") : string.Empty;

            //var notification = new Notification {
            //    TickerText = new Java.Lang.String("Song started!"),
            //    Icon = Resource.Drawable.logo
            //};
            //notification.Flags |= NotificationFlags.OngoingEvent;
            //notification.SetLatestEventInfo(ApplicationContext, "Xamarin Streaming", "Playing music!", pendingIntent);

            var notification = new Notification.Builder(this, channelId)
                .SetContentTitle("MusicShare")
                .SetContentText(contentText)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetContentIntent(pendingIntent)
                .SetCategory(Notification.CategoryService)
                .SetOngoing(true)
                .SetBadgeIconType(NotificationBadgeIconType.Small)
                // .AddAction(BuildRestartTimerAction())
                // .AddAction(BuildStopServiceAction())
                .Build();
            
            return notification;
        }

        private string CreateNotificationChannel(string channelId, string channelName)
        {
            var chan = new NotificationChannel(channelId, channelName, NotificationImportance.None);
            // chan.LightColor = Color.BLUE;
            chan.LockscreenVisibility = NotificationVisibility.Public;

            var service = this.GetSystemService(Context.NotificationService) as NotificationManager;
            service.CreateNotificationChannel(chan);
            return channelId;
        }

        /// <summary>
        /// Lock the wifi so we can still stream under lock screen
        /// </summary>
        private void AquireWifiLock()
        {
            if (_wifiLock == null)
            {
                _wifiLock = _wifiManager.CreateWifiLock(WifiMode.Full, "xamarin_wifi_lock");
            }
            _wifiLock.Acquire();
        }

        /// <summary>
        /// This will release the wifi lock if it is no longer needed
        /// </summary>
        private void ReleaseWifiLock()
        {
            if (_wifiLock == null)
                return;

            _wifiLock.Release();
            _wifiLock = null;
        }

        /// <summary>
        /// Properly cleanup of your player by releasing resources
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            this.UnregisterReceiver(_intentReceiver);
            _player.SafeDispose();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                this.StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
                this.StopForeground(true);
            }

            this.StopSelf();
        }

        /// <summary>
        /// For a good user experience we should account for when audio focus has changed.
        /// There is only 1 audio output there may be several media services trying to use it so
        /// we should act correctly based on this.  "duck" to be quiet and when we gain go full.
        /// All applications are encouraged to follow this, but are not enforced.
        /// </summary>
        /// <param name="focusChange"></param>
        void AudioManager.IOnAudioFocusChangeListener.OnAudioFocusChange(AudioFocus focusChange)
        {
            //switch (focusChange)
            //{
            //    case AudioFocus.Gain:
            //        if (_player == null)
            //            this.IntializePlayer();

            //        if (!_player.IsPlaying)
            //        {
            //            _player.Start();
            //            _paused = false;
            //        }

            //        _player.SetVolume(1.0f);//Turn it up!
            //        break;
            //    case AudioFocus.Loss:
            //        //We have lost focus stop!
            //        this.Stop();
            //        break;
            //    case AudioFocus.LossTransient:
            //        //We have lost focus for a short time, but likely to resume so pause
            //        this.Pause();
            //        break;
            //    case AudioFocus.LossTransientCanDuck:
            //        //We have lost focus but should till play at a muted 10% volume
            //        if (_player.IsPlaying)
            //            _player.SetVolume(.1f);//turn it down!
            //        break;

            //}
        }

        #endregion
    }
}