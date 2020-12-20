using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace MusicShare.ViewModels.Home
{
    class TrackInfo : BindableObject
    {
        #region bool IsActive 

        public bool IsActive
        {
            get { return (bool)this.GetValue(IsActiveProperty); }
            set { this.SetValue(IsActiveProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsActive. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsActiveProperty =
            BindableProperty.Create("IsActive", typeof(bool), typeof(TrackInfo), default(bool));

        #endregion

        #region bool IsSelected 

        public bool IsSelected
        {
            get { return (bool)this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsSelected. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsSelectedProperty =
            BindableProperty.Create("IsSelected", typeof(bool), typeof(TrackInfo), default(bool));

        #endregion

        public PlayerTrackInfo Info { get; }

        public string Header { get; }
        public string Duration { get; }

        public PlaybackViewModel Owner { get; }

        public TrackInfo(PlaybackViewModel owner, PlayerTrackInfo info)
        {
            this.Owner = owner;
            this.Info = info;

            var fname = System.IO.Path.GetFileName(info.FilePathOrUri);
            this.Header = (string.IsNullOrWhiteSpace(info.Artist) || string.IsNullOrWhiteSpace(info.Title)) ? fname : info.Artist + " - " + info.Title;

            this.Duration = info.Duration.Minutes.ToString().PadLeft(2, '0') + ":" + info.Duration.Seconds.ToString().PadLeft(2, '0');

            if (info.Duration > TimeSpan.FromHours(1))
            {
                this.Duration = info.Duration.Truncate(TimeSpan.FromHours(1)).TotalHours.ToString() + ":" + this.Duration;
            }
        }
    }

    class PlaybackViewModel : MenuPageViewModel
    {
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        public ICommand NextTrackCommand { get; }
        public ICommand PrevTrackCommand { get; }

        public ICommand AddTrackCommand { get; }
        public ICommand SelectTrackCommand { get; }

        public ICommand SelectCancelTrackCommand { get; }
        public ICommand SelectRemoveTrackCommand { get; }

        #region ObservableCollection<TrackInfo> Tracklist 

        public ObservableCollection<TrackInfo> Tracklist
        {
            get { return (ObservableCollection<TrackInfo>)this.GetValue(TracklistProperty); }
            set { this.SetValue(TracklistProperty, value); }
        }

        // Using a BindableProperty as the backing store for Tracklist. This enables animation, styling, binding, etc...
        public static readonly BindableProperty TracklistProperty =
            BindableProperty.Create("Tracklist", typeof(ObservableCollection<TrackInfo>), typeof(PlaybackViewModel), default(ObservableCollection<TrackInfo>));

        #endregion

        #region TrackInfo ActiveTrack 

        public TrackInfo ActiveTrack
        {
            get { return (TrackInfo)this.GetValue(ActiveTrackProperty); }
            set { this.SetValue(ActiveTrackProperty, value); }
        }

        // Using a BindableProperty as the backing store for ActiveTrack. This enables animation, styling, binding, etc...
        public static readonly BindableProperty ActiveTrackProperty =
            BindableProperty.Create("ActiveTrack", typeof(TrackInfo), typeof(PlaybackViewModel), default(TrackInfo));

        #endregion

        #region bool PlayCmdAvailable 

        public bool PlayCmdAvailable
        {
            get { return (bool)this.GetValue(PlayCmdAvailableProperty); }
            set { this.SetValue(PlayCmdAvailableProperty, value); }
        }

        // Using a BindableProperty as the backing store for PlayCmdAvailable. This enables animation, styling, binding, etc...
        public static readonly BindableProperty PlayCmdAvailableProperty =
            BindableProperty.Create("PlayCmdAvailable", typeof(bool), typeof(PlaybackViewModel), default(bool));

        #endregion

        #region bool PauseCmdAvailable 

        public bool PauseCmdAvailable
        {
            get { return (bool)this.GetValue(PauseCmdAvailableProperty); }
            set { this.SetValue(PauseCmdAvailableProperty, value); }
        }

        // Using a BindableProperty as the backing store for PauseCmdAvailable. This enables animation, styling, binding, etc...
        public static readonly BindableProperty PauseCmdAvailableProperty =
            BindableProperty.Create("PauseCmdAvailable", typeof(bool), typeof(PlaybackViewModel), default(bool));

        #endregion

        #region bool StopCmdAvailable 

        public bool StopCmdAvailable
        {
            get { return (bool)this.GetValue(StopCmdAvailableProperty); }
            set { this.SetValue(StopCmdAvailableProperty, value); }
        }

        // Using a BindableProperty as the backing store for StopCmdAvailable. This enables animation, styling, binding, etc...
        public static readonly BindableProperty StopCmdAvailableProperty =
            BindableProperty.Create("StopCmdAvailable", typeof(bool), typeof(PlaybackViewModel), default(bool));

        #endregion

        #region string StatusString 

        public string StatusString
        {
            get { return (string)this.GetValue(StatusStringProperty); }
            set { this.SetValue(StatusStringProperty, value); }
        }

        // Using a BindableProperty as the backing store for StatusString. This enables animation, styling, binding, etc...
        public static readonly BindableProperty StatusStringProperty =
            BindableProperty.Create("StatusString", typeof(string), typeof(PlaybackViewModel), default(string));

        #endregion

        #region string TracklistStatusString 

        public string TracklistStatusString
        {
            get { return (string)this.GetValue(TracklistStatusStringProperty); }
            set { this.SetValue(TracklistStatusStringProperty, value); }
        }

        // Using a BindableProperty as the backing store for TracklistStatusString. This enables animation, styling, binding, etc...
        public static readonly BindableProperty TracklistStatusStringProperty =
            BindableProperty.Create("TracklistStatusString", typeof(string), typeof(PlaybackViewModel), default(string));

        #endregion

        #region bool ShowSelectors 

        public bool ShowSelectors
        {
            get { return (bool)this.GetValue(ShowSelectorsProperty); }
            set { this.SetValue(ShowSelectorsProperty, value); }
        }

        // Using a BindableProperty as the backing store for ShowSelectors. This enables animation, styling, binding, etc...
        public static readonly BindableProperty ShowSelectorsProperty =
            BindableProperty.Create("ShowSelectors", typeof(bool), typeof(PlaybackViewModel), default(bool));

        #endregion

        public PlaybackViewModel(AppViewModel app)
            : base("Playback")
        {
            var player = ServiceContext.Instance.Player;

            this.PlayCmdAvailable = true;
            this.PauseCmdAvailable = false;
            this.StopCmdAvailable = true; // false;
            this.StatusString = "...";

            this.PlayCommand = new Command(async () => player.Start());
            this.PauseCommand = new Command(async () => player.Pause());
            this.StopCommand = new Command(async () => player.Stop());

            this.NextTrackCommand = new Command(async () => player.PlayNextTrack());
            this.PrevTrackCommand = new Command(async () => player.PlayPrevTrack());

            this.AddTrackCommand = new Command(async () => {
                var f = await Plugin.FilePicker.CrossFilePicker.Current.PickFile(new[] { "*.mp3" });
                if (f != null && !string.IsNullOrEmpty(f.FilePath))
                {
                    player.Playlist.Add(f.FilePath);
                    this.UpdateTracklistStatus();
                }
            });
            this.SelectTrackCommand = new Command(async () => {
                this.ShowSelectors = true;
            });
            this.SelectCancelTrackCommand = new Command(async () => {
                this.ResetSelection();
            });
            this.SelectRemoveTrackCommand = new Command(async () => {
                var tracks = this.Tracklist;
                for (int i = 0; i < tracks.Count;)
                {
                    if (tracks[i].IsSelected)
                    {
                        player.Playlist.Remove(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                this.UpdateTracklistStatus();
                this.ResetSelection();
            });

            this.Tracklist = new ObservableCollection<TrackInfo>();
            player.Playlist.OnClear += () => this.InvokeAction(() => {
                this.Tracklist.Clear();
                this.UpdateTracklistStatus();
            });
            player.Playlist.OnInsertItem += (n, e) => this.InvokeAction(() => {
                this.Tracklist.Insert(n, new TrackInfo(this, e));
                this.UpdateTracklistStatus();
            });
            player.Playlist.OnRemoveItem += (n) => this.InvokeAction(() => {
                this.Tracklist.RemoveAt(n);
                this.UpdateTracklistStatus();
            });
            player.Playlist.OnActiveItemChanged += (n) => this.InvokeAction(() => {
                if (this.ActiveTrack != null)
                    this.ActiveTrack.IsActive = false;

                var track = this.Tracklist[n];
                track.IsActive = true;
                this.ActiveTrack = track;
            });
        }

        private void ResetSelection()
        {
            this.ShowSelectors = false;
            var tracks = this.Tracklist;
            for (int i = 0; i < tracks.Count;)
            {
                tracks[i].IsSelected = false;
            }
        }

        private void UpdateTracklistStatus()
        {
            this.TracklistStatusString = this.Tracklist.Count + " tracks";
        }
    }

}
