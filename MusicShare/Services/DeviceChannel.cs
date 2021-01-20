using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MusicShare.Interaction.Standard;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Interaction.Standard.Stream;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using MusicShare.Services.Streams;
using MusicShare.Shared.Common;

namespace MusicShare.Services
{
    public interface IDeviceChannel : IDisposable
    {
        string RemotePeerName { get; }

        event Action OnClosed;

        System.IO.Stream Stream { get; }

        event Action OnEnabled;
        event Action OnDisabled;
        event Action<TrackInfoPacket> OnTrackInfo;
        event Action<StampedStreamDataPacket> OnStreamData;

        bool IsPlayingTo { get; }
        bool IsPlayingFrom { get; }


        void StartListening();
        void StopListening();
        void PostInfo(int streamId, PlayerTrackInfo info);
        void Start();
        void SendData(StampedStreamDataPacket pckt);
    }

    public interface IBtDeviceChannel : IDeviceChannel
    {
        BtDeviceEntryInfo Info { get; }
    }

    public interface INetDeviceChannel : IDeviceChannel
    {
        NetHostInfo Info { get; }
    }

    public abstract class DeviceChannel : DisposableObject, IDeviceChannel, IPacketHandler<object>
    {
        public string RemotePeerName { get; }

        public event Action OnClosed;

        public event Action OnEnabled;
        public event Action OnDisabled;
        public event Action<TrackInfoPacket> OnTrackInfo;
        public event Action<StampedStreamDataPacket> OnStreamData;

        public bool IsPlayingTo { get; private set; }
        public bool IsPlayingFrom { get; private set; }

        public Stream Stream { get; }
        public StreamAsyncWriter AsyncSender { get; }

        protected readonly DisposableList _disposables = new DisposableList();

        protected readonly byte[] _buff = new byte[8192];
        protected readonly AsyncCallback _onDataProc;

        public DeviceChannel(Stream stream, StreamAsyncWriter sender, string remotePeerName)
        {
            _onDataProc = this.OnData;
            this.Stream = _disposables.Add(stream);
            this.AsyncSender = sender;
            this.RemotePeerName = remotePeerName;
        }

        public void Start()
        {
            this.Stream.BeginRead(_buff, 0, 2, _onDataProc, null);
        }

        private void OnData(IAsyncResult ar)
        {
            int read;
            try { read = this.Stream.EndRead(ar); }
            catch { read = 0; }

            if (read == 2 || (read == 1 && this.Stream.TryRead(_buff, 1, 1)))
            {
                try
                {
                    var packetLength = _buff.UnpackInt16(0);
                    if (this.Stream.TryRead(_buff, 2, packetLength - 2))
                    {
                        var packet = Packet.Parse(_buff, 0, packetLength);
                        if (packet != null)
                        {
                            try
                            {
                                packet.HandleWith(this);
                            }
                            catch (Exception ex)
                            {
                                Log.TraceMethod(ex.ToString());
                            }

                            this.Stream.BeginRead(_buff, 0, 1, _onDataProc, null);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.TraceMethod(ex.ToString());
                }
            }

            this.SafeDispose();
        }

        public void SendData(StampedStreamDataPacket packet)
        {
            this.Send(packet);
        }

        private void Send(Packet packet)
        {
            if (!this.IsDisposed)
            {
                this.AsyncSender.SendAsync(packet.ToArray());
            }
        }

        public void StartListening()
        {
            this.IsPlayingFrom = true;
            this.Send(new PlaybackControlPacket() { Operation = PlaybackControlOperation.Start });
        }

        public void StopListening()
        {
            this.IsPlayingFrom = false;
            this.Send(new PlaybackControlPacket() { Operation = PlaybackControlOperation.Stop });
        }

        public void PostInfo(int streamId, PlayerTrackInfo info)
        {
            this.Send(new TrackInfoPacket() { AuthorPlaying = info.Artist, TitlePlaying = info.Title, StreamId = streamId });
        }

        protected sealed override void DisposeImpl()
        {
            _disposables.SafeDispose();

            try { this.OnClosed?.Invoke(); }
            catch (Exception ex) { Log.TraceMethod(ex.ToString()); }
        }

        object IPacketHandler<object>.HandleStreamInfo(TrackInfoPacket pckt)
        {
            this.OnTrackInfo?.Invoke(pckt);
            return null;
        }

        object IPacketHandler<object>.HandleStreamHead(StreamDataHeadPacket pckt) { return null; }

        object IPacketHandler<object>.HandleStreamBody(StreamDataBodyPacket pckt) { return null; }

        object IPacketHandler<object>.HandleStreamTail(StreamDataTailPacket pckt) { return null; }

        object IPacketHandler<object>.HandleStreamHead(StampedStreamDataHeadPacket pckt)
        {
            this.OnStreamData?.Invoke(pckt);
            return null;
        }

        object IPacketHandler<object>.HandleStreamBody(StampedStreamDataBodyPacket pckt)
        {
            this.OnStreamData?.Invoke(pckt);
            return null;
        }

        object IPacketHandler<object>.HandleStreamTail(StampedStreamDataTailPacket pckt)
        {
            this.OnStreamData?.Invoke(pckt);
            return null;
        }

        object IPacketHandler<object>.HandleControl(PlaybackControlPacket pckt)
        {
            switch (pckt.Operation)
            {
                case PlaybackControlOperation.Start: this.IsPlayingTo = true; this.OnEnabled?.Invoke(); break;
                case PlaybackControlOperation.Stop: this.IsPlayingTo = false; this.OnDisabled?.Invoke(); break;
                default:
                    break;
            }
            return null;
        }
    }
}
