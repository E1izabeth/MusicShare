using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MusicShare.Droid.Services.Impl
{

    class PlayerPlaylist : DisposableObject, IPlayerPlaylist
    {
        public event Action OnClear;
        public event Action<int> OnRemoveItem;
        public event Action<int, PlayerTrackInfo> OnInsertItem;
        public event Action<int> OnActiveItemChanged;

        readonly Context _context;

        readonly List<PlayerTrackInfo> _playerTracks = new List<PlayerTrackInfo>();

        public int ActiveTrackIndex { get; private set; }
        public bool IsEmpty { get { return _playerTracks.Count == 0; } }

        public PlayerPlaylist(Context context)
        {
            _context = context;
        }

        public void Enumerate()
        {
            this.OnClear?.Invoke();

            for (int i = 0; i < _playerTracks.Count; i++)
            {
                this.OnInsertItem?.Invoke(i, _playerTracks[i]);
            }

            if (_playerTracks.Count > 0)
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
        }

        public PlayerTrackInfo Get(int index)
        {
            return _playerTracks[index];
        }

        public void Add(string filepath)
        {
            var trackInfo = this.CollectTrackInfo(filepath);
            var index = _playerTracks.Count;
            _playerTracks.Add(trackInfo);
            this.OnInsertItem?.Invoke(index, trackInfo);
        }

        public void Remove(int index)
        {
            _playerTracks.RemoveAt(index);
            this.OnRemoveItem?.Invoke(index);

            {
                var active = this.ActiveTrackIndex;
                if (active == index) // TODO consider this case
                {
                    active = index - 1;
                    this.ActiveTrackIndex = active;
                }
                else if (active > index)
                {
                    active--;
                    this.ActiveTrackIndex = active;
                    this.OnActiveItemChanged?.Invoke(active);
                }
            }
        }

        public void Move(int from, int to)
        {
            var track = _playerTracks[from];
            _playerTracks.RemoveAt(from);
            this.OnRemoveItem?.Invoke(from);

            var actualTo = to > from ? to - 1 : to;
            _playerTracks.Insert(actualTo, track);
            this.OnInsertItem?.Invoke(actualTo, track);

            {
                var active = this.ActiveTrackIndex;
                if (active == from)
                    active = to;
                else if (active > from)
                    active--;

                if (active >= actualTo)
                    active++;

                this.ActiveTrackIndex = active;
                this.OnActiveItemChanged?.Invoke(active);
            }
        }

        public bool TryRevert()
        {
            if (this.ActiveTrackIndex - 1 >= 0)
            {
                this.ActiveTrackIndex--;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryAdvance()
        {
            if (this.ActiveTrackIndex + 1 < _playerTracks.Count)
            {
                this.ActiveTrackIndex++;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryActivate(int index)
        {
            if (index >= 0 && index < _playerTracks.Count)
            {
                this.ActiveTrackIndex = index;
                this.OnActiveItemChanged?.Invoke(this.ActiveTrackIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            _playerTracks.Clear();
            this.ActiveTrackIndex = 0;
            this.OnClear?.Invoke();
            this.OnActiveItemChanged?.Invoke(0);
        }

        private PlayerTrackInfo CollectTrackInfo(string filePathOrUri)
        {
            var metadataRetriever = new MediaMetadataRetriever();

            if (File.Exists(filePathOrUri))
            {
                metadataRetriever.SetDataSource(filePathOrUri);
            }
            else
            {
                var uri = Android.Net.Uri.Parse(filePathOrUri);
                metadataRetriever.SetDataSource(_context, uri);
            }

            var album = metadataRetriever.ExtractMetadata(MetadataKey.Album);

            var artists = new[] {
                metadataRetriever.ExtractMetadata(MetadataKey.Albumartist),
                metadataRetriever.ExtractMetadata(MetadataKey.Artist),
                metadataRetriever.ExtractMetadata(MetadataKey.Author),
                metadataRetriever.ExtractMetadata(MetadataKey.Composer),
                metadataRetriever.ExtractMetadata(MetadataKey.Writer),
            };
            var artist = artists.FirstOrDefault(s => !string.IsNullOrEmpty(s));

            var trackNumberStr = metadataRetriever.ExtractMetadata(MetadataKey.CdTrackNumber);
            var trackNumber = !string.IsNullOrEmpty(trackNumberStr) && int.TryParse(trackNumberStr, out var trackNumberResult) ? trackNumberResult : default(int?);

            var durationStr = metadataRetriever.ExtractMetadata(MetadataKey.Duration);
            var duration = !string.IsNullOrEmpty(durationStr) && int.TryParse(durationStr, out var durationResult) ? TimeSpan.FromMilliseconds(durationResult) : default;

            var title = metadataRetriever.ExtractMetadata(MetadataKey.Title);

            metadataRetriever.SafeDispose();

            return new PlayerTrackInfo(album, trackNumber, artist, duration, title, filePathOrUri);
        }

        protected override void DisposeImpl()
        {
        }
    }
}