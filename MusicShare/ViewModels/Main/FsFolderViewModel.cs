using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using MusicShare.Services.Platform;
using Xamarin.Forms;

namespace MusicShare.ViewModels.Main
{
    public class FsBrowsingContext
    {
        public MenuPageViewModel BackPage { get; set; }
        // public string FilePattern { get; set; }
        public Action<IEnumerable<IPlatformFsItem>> OkCallback { get; set; }

        public FsBrowsingContext()
        {
        }
    }

    public class FsItem : BindableObject
    {
        #region bool IsSelected 

        public bool IsSelected
        {
            get { return (bool)this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsSelectedProperty =
            BindableProperty.Create("IsSelected", typeof(bool), typeof(FsItem), default(bool));

        #endregion

        public FsFolderViewModel Owner { get; }

        public IPlatformFsItem PlatformFsItem { get; }

        public bool IsFile { get { return !this.PlatformFsItem.IsDir; } }

        public string Name { get; }
        public string Extension { get; }

        public ICommand ToggleCommand { get; }

        public FsItem(FsFolderViewModel owner, IPlatformFsItem fsItem)
        {
            this.Owner = owner;
            this.PlatformFsItem = fsItem;

            this.Name = fsItem.IsDir ? fsItem.Name : Path.GetFileNameWithoutExtension(fsItem.Name);
            this.Extension = fsItem.IsDir ? "(...)" : (Path.HasExtension(fsItem.Name) ? Path.GetExtension(fsItem.Name) : string.Empty);

            this.ToggleCommand = new Command(() => {
                if (this.PlatformFsItem.IsDir)
                {
                    owner.OpenDir(this.PlatformFsItem);
                }
                else
                {
                    this.IsSelected = !this.IsSelected;
                }
            });
        }
    }

    public class FsFolderViewModel : MenuPageViewModel
    {
        public bool BackCommandAvailable { get; private set; }
        public ICommand BackCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }
        public ICommand OkCommand { get; private set; }

        #region ObservableCollection<FsItem> Entries 

        public ObservableCollection<FsItem> Entries
        {
            get { return (ObservableCollection<FsItem>)this.GetValue(EntriesProperty); }
            set { this.SetValue(EntriesProperty, value); }
        }

        // Using a BindableProperty as the backing store for Entries.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty EntriesProperty =
            BindableProperty.Create("Entries", typeof(ObservableCollection<FsItem>), typeof(FsFolderViewModel), default(ObservableCollection<FsItem>));

        #endregion

        public FsBrowsingContext Context { get; private set; }

        // readonly Action _refresh;
        private readonly Func<IPlatformFsItem[]> _getDirsProc = null;
        private readonly Func<IPlatformFsItem[]> _getFilesProc = null;
        private readonly FsFolderViewModel _parent;

        public FsFolderViewModel(FsBrowsingContext context, AppStateGroupViewModel group)
            : base("Places", group)
        {
            this.BackCommandAvailable = false;
            _parent = null;

            // _refresh = () => { };
            //_getDirsProc = () => DriveInfo.GetDrives().Select(d => d.RootDirectory).ToArray();
            _getDirsProc = () => PlatformContext.Instance.GetFsRoots();
            _getFilesProc = () => new IPlatformFsItem[0];

            this.Initialize(context);

            PlatformContext.Instance.DemandFsPermission();
            this.OnRefresh();
        }

        public FsFolderViewModel(IPlatformFsItem dir, FsBrowsingContext context, FsFolderViewModel parent, AppStateGroupViewModel group)
            : base(dir.Name, group)
        {
            this.BackCommandAvailable = true;
            _parent = parent;

            // _refresh = () => dir.Refresh();
            // _getDirsProc = () => dir.GetDirectories();
            // _getFilesProc = () => string.IsNullOrWhiteSpace(this.Context.FilePattern) ? dir.GetFiles() : dir.GetFiles(this.Context.FilePattern);

            _getDirsProc = () => dir.GetDirs();
            _getFilesProc = () => dir.GetFiles();

            this.Initialize(context);
            this.OnRefresh();
        }

        private void Initialize(FsBrowsingContext context)
        {
            this.IsRefreshAvailable = true;
            this.Context = context;
            this.Entries = new ObservableCollection<FsItem>();

            this.BackCommand = new Command(() => {
                AppViewModel.Instance.CurrentStateModel.CurrentPage = _parent;
            });
            this.CancelCommand = new Command(() => {
                AppViewModel.Instance.OperationInProgress = true;

                AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                    AppViewModel.Instance.CurrentStateModel.CurrentGroup = this.Context.BackPage.Group;

                    AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                        AppViewModel.Instance.CurrentStateModel.CurrentPage = this.Context.BackPage;

                        AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                            AppViewModel.Instance.OperationInProgress = false;
                        });
                    });
                });
            });
            this.OkCommand = new Command(() => {
                AppViewModel.Instance.OperationInProgress = true;
                var selectedItems = this.Entries.Where(e => e.IsSelected).Select(e => e.PlatformFsItem).ToArray();

                AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                    AppViewModel.Instance.CurrentStateModel.CurrentGroup = this.Context.BackPage.Group;

                    AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                        AppViewModel.Instance.CurrentStateModel.CurrentPage = this.Context.BackPage;

                        AppViewModel.Instance.Dispatcher.BeginInvokeOnMainThread(() => {
                            this.Context.OkCallback?.Invoke(selectedItems);
                            AppViewModel.Instance.OperationInProgress = false;
                        });
                    });
                });
            });
        }

        public override void OnRefresh()
        {
            this.IsRefreshing = true;

            this.Entries.Clear();

            // _refresh();

            try
            {
                var childDirs = _getDirsProc();
                foreach (var dir in childDirs)
                    this.Entries.Add(new FsItem(this, dir));

                var childFiles = _getFilesProc();
                foreach (var file in childFiles)
                    this.Entries.Add(new FsItem(this, file));
            }
            catch (Exception ex)
            {
                AppViewModel.Instance.PostError(ex.Message);
            }

            this.IsRefreshing = false;
        }

        private void Close(int step = 0)
        {
            var index = this.Group.SiblingPages.IndexOf(this);

            for (int i = this.Group.SiblingPages.Count - 1; i >= index + 1 && i > 0; i--)
                this.Group.SiblingPages.RemoveAt(i);
        }

        public void OpenDir(IPlatformFsItem dirItem)
        {
            this.Close(1);
            var nextPage = new FsFolderViewModel(dirItem, this.Context, this, this.Group);
            AppViewModel.Instance.CurrentStateModel.CurrentPage = nextPage;
        }
    }
}
