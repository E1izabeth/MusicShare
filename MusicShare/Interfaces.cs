using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;

namespace MusicShare
{
    public class PlayerTrackInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Album { get; private set; }
        public int? TrackNumber { get; private set; }
        public string Artist { get; private set; }
        public TimeSpan? Duration { get; private set; }
        public string Title { get; private set; }
        public string FilePathOrUri { get; private set; }
        public IDeviceChannel Channel { get; private set; }

        public PlayerTrackInfo(string album, int? trackNumber, string artist, TimeSpan? duration, string title, string filePathOrUri)
        {
            this.Album = album;
            this.TrackNumber = trackNumber;
            this.Artist = artist;
            this.Duration = duration;
            this.Title = title;
            this.FilePathOrUri = filePathOrUri;
            this.Channel = null;
        }

        public PlayerTrackInfo(IDeviceChannel channel)
        {
            this.Album = null;
            this.TrackNumber = null;
            this.Artist = "[" + channel.RemotePeerName + "]";
            this.Duration = null;
            this.Title = "...";
            this.FilePathOrUri = null;
            this.Channel = channel;
            this.Channel.OnTrackInfo += pckt => {
                this.Artist = "[" + channel.RemotePeerName + "] " + pckt.AuthorPlaying;
                this.Title = pckt.TitlePlaying;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Artist"));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            };
        }
    }

    public interface IPlayerPlaylist
    {
        event Action OnClear;
        event Action<int> OnRemoveItem;
        event Action<int, PlayerTrackInfo> OnInsertItem;
        event Action<int> OnActiveItemChanged;
        event Action<int, PlayerTrackInfo> OnUpdateItem;

        int ActiveTrackIndex { get; }

        void Add(string filepath);
        void Remove(int index);
        void Move(int from, int to);
        void Clear();

        void Enumerate();
    }

    public enum PlayerState
    {
        Stopped,
        Playing,
        Paused
    }

    public interface IPlayer
    {
        event Action<IDeviceChannel> OnConnection;

        event Action OnStateChanged;
        event Action OnPositionChanged;

        IBluetoothConnector BtConnector { get; }
        INetworkConnector NetConnector { get; }
        IPlayerPlaylist Playlist { get; }

        PlayerState State { get; }

        bool IsPlaying { get; }
        bool IsPaused { get; }
        bool IsStopped { get; }
        TimeSpan CurrentDuration { get; }
        TimeSpan CurrentPosition { get; }

        void Start();
        void Pause();
        void Stop();
        void SetVolume(float volume);
        void PlayNextTrack();
        void PlayPrevTrack();
        void JumpToTrack(int index);

        ConnectivityInfoType GetConnectivityInfo();
        void Connect(ConnectivityInfoType target, Action<bool> callback);
    }

    public interface IPlayerService
    {
        IPlayer Player { get; }

        void Terminate();
    }

    public interface IActivity
    {
        IPlayer Player { get; }

        void Terminate();
    }

    public class ServiceContext
    {
        public static IPlayerService Instance { get; private set; }

        public static void SetInstance(IPlayerService service)
        {
            Instance = service;
        }
    }

    public class ActivityContext
    {
        public static IActivity Instance { get; private set; }

        public static void SetInstance(IActivity activity)
        {
            Instance = activity;
        }
    }
}
