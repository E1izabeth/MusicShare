using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicShare
{
    public class BtDeviceEntryInfo
    {
        public string Address { get; private set; }
        public string Name { get; private set; }

        public BtDeviceEntryInfo(string address, string name)
        {
            this.Address = address;
            this.Name = name;
        }
    }

    public interface IBluetoothConnector
    {
        event Action OnActivated;
        event Action OnDeactivate;

        event Action OnDiscoverReset;
        event Action<BtDeviceEntryInfo> OnDiscoverFound;

        // event Action<BtDeviceConnection> OnNewConnection;

        bool IsEnabled { get; }
        bool IsDiscovering { get; }


        void RefreshDevices();
        bool ConnectBt(string addr);
    }

    public class NetHostInfo
    {
        public string Name { get; private set; }
        public string Address { get; private set; }
        public TimeSpan Ping { get; private set; }
        public ushort Port { get; private set; }

        public NetHostInfo(string name, string address, TimeSpan ping, ushort port)
        {
            this.Name = name;
            this.Address = address;
            this.Ping = ping;
            this.Port = port;
        }
    }

    public interface INetworkConnector
    {
        event Action OnDiscoverReset;
        event Action<NetHostInfo> OnDiscoverFound;

        // event Action<NetDeviceConnection> OnNewConnection;

        bool IsEnabled { get; }

        void RefreshHosts();

        bool ConnectTo(string host, ushort port);
    }

    public class PlayerTrackInfo
    {
        public string Album { get; private set; }
        public int? TrackNumber { get; private set; }
        public string Artist { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string Title { get; private set; }
        public string FilePathOrUri { get; private set; }

        public PlayerTrackInfo(string album, int? trackNumber, string artist, TimeSpan duration, string title, string filePathOrUri)
        {
            this.Album = album;
            this.TrackNumber = trackNumber;
            this.Artist = artist;
            this.Duration = duration;
            this.Title = title;
            this.FilePathOrUri = filePathOrUri;
        }
    }

    public interface IPlayerPlaylist
    {
        event Action OnClear;
        event Action<int> OnRemoveItem;
        event Action<int, PlayerTrackInfo> OnInsertItem;
        event Action<int> OnActiveItemChanged;

        int ActiveTrackIndex { get; }

        void Add(string filepath);
        void Remove(int index);
        void Move(int from, int to);
        void Clear();
    }

    public enum PlayerState
    {
        Stopped,
        Playing,
        Paused
    }

    public interface IDeviceChannel
    {
        System.IO.Stream Stream { get; }
    }

    public interface IBtDeviceChannel : IDeviceChannel
    {
        BtDeviceEntryInfo Info { get; }
    }

    public interface INetDeviceChannel : IDeviceChannel
    {
        NetHostInfo Info { get; }
    }

    public interface IPlayer
    {
        event Action OnStateChanged;
        event Action OnPositionChanged;
        
        event Action<IDeviceChannel> OnConnection;

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
        void Connect(ConnectivityInfoType target, Action callback);
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
}
