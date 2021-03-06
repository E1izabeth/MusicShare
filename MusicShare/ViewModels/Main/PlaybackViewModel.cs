﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using MusicShare.Services.Platform;
using MusicShare.ViewModels.Main;
using Xamarin.Forms;

namespace MusicShare.ViewModels.Home
{
    public class TrackInfo : BindableObject
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

            if (info.Duration.HasValue)
            {
                this.Duration = info.Duration.Value.FormatPlaybackTime();
            }
            else
            {
                this.Duration = "--:--";
            }
        }
    }

    public class PlaybackViewModel : MenuPageViewModel
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

        #region TrackInfo SelectedTrack 

        public TrackInfo SelectedTrack
        {
            get { return (TrackInfo)this.GetValue(SelectedTrackProperty); }
            set { this.SetValue(SelectedTrackProperty, value); }
        }

        // Using a BindableProperty as the backing store for SelectedTrack. This enables animation, styling, binding, etc...
        public static readonly BindableProperty SelectedTrackProperty =
            BindableProperty.Create("SelectedTrack", typeof(TrackInfo), typeof(PlaybackViewModel), default(TrackInfo), propertyChanged: OnSelectedTrackPropertyChanged);

        private static void OnSelectedTrackPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PlaybackViewModel model)
                model.OnSelectedTrackChanged(oldValue as TrackInfo, newValue as TrackInfo);
        }

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

        #region double DurationProgress 

        public double DurationProgress
        {
            get { return (double)this.GetValue(DurationProgressProperty); }
            set { this.SetValue(DurationProgressProperty, value); }
        }

        // Using a BindableProperty as the backing store for DurationProgress. This enables animation, styling, binding, etc...
        public static readonly BindableProperty DurationProgressProperty =
            BindableProperty.Create("DurationProgress", typeof(double), typeof(PlaybackViewModel), default(double));

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

        public PlaybackViewModel(AppStateGroupViewModel group)
            : base("Playback", group)
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

            var ctx = new FsBrowsingContext() {
                BackPage = this,
                OkCallback = fsItems => {
                    IPlatformFsItem[] expand(IPlatformFsItem item) => item.IsDir ? item.GetDirs().SelectMany(expand).Concat(item.GetFiles()).ToArray() : new[] { item };
                    var items = fsItems.SelectMany(expand).ToArray();
                    foreach (var item in items.Where(f => MimeTypes.GetMimeType(f.Name).StartsWith("audio/")))
                        player.Playlist.Add(item.Path);

                    this.UpdateTracklistStatus();
                }
            };
            var fsBrowsingPages = new AppStateGroupViewModel();
            var fsRootPage = new FsFolderViewModel(ctx, fsBrowsingPages);

            this.AddTrackCommand = new Command(async () => {
                //var f = await Plugin.FilePicker.CrossFilePicker.Current.PickFile(new[] { "audio/*" });
                //if (f != null && !string.IsNullOrEmpty(f.FilePath))
                //{
                //    player.Playlist.Add(f.FilePath);
                //    this.UpdateTracklistStatus();
                //}
                AppViewModel.Instance.CurrentStateModel.CurrentGroup = fsBrowsingPages;
                AppViewModel.Instance.CurrentStateModel.CurrentPage = fsBrowsingPages.SiblingPages.Last();
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
            player.OnStateChanged += () => this.InvokeAction(() => {
                this.PlayCmdAvailable = player.IsPaused || player.IsStopped;
                this.PauseCmdAvailable = player.IsPlaying;
                this.StopCmdAvailable = player.IsPlaying || player.IsPaused;

                if (player.IsStopped)
                {
                    this.StatusString = "...";
                    this.DurationProgress = 0;
                }
            });
            player.OnPositionChanged += () => this.InvokeAction(() => {
                this.StatusString = player.CurrentPosition.FormatPlaybackTime();
                this.DurationProgress = player.CurrentPosition.TotalMilliseconds / player.CurrentDuration.TotalMilliseconds;
            });
            player.Playlist.Enumerate();
        }

        private void OnSelectedTrackChanged(TrackInfo oldTrack, TrackInfo newTrack)
        {
            if (newTrack != null)
            {
                var index = this.Tracklist.IndexOf(newTrack);
                ServiceContext.Instance.Player.JumpToTrack(index);
                this.SelectedTrack = null;
            }
        }

        private void ResetSelection()
        {
            this.ShowSelectors = false;
            var tracks = this.Tracklist;
            for (int i = 0; i < tracks.Count; i++)
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
