using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MusicShare.Models;
using MusicShare.ViewModels.Home;
using MusicShare.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public abstract class MenuPageViewModel : BindableObject
    {
        public AppViewModel App { get { return AppViewModel.Instance; } }

        public ICommand RefreshCommand { get; }

        #region bool IsRefreshAvailable 

        public bool IsRefreshAvailable
        {
            get { return (bool)this.GetValue(IsRefreshAvailableProperty); }
            set { this.SetValue(IsRefreshAvailableProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsRefreshAvailable.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsRefreshAvailableProperty =
            BindableProperty.Create("IsRefreshAvailable", typeof(bool), typeof(MenuPageViewModel), default(bool));

        #endregion

        #region bool IsRefreshing 

        public bool IsRefreshing
        {
            get { return (bool)this.GetValue(IsRefreshingProperty); }
            set { this.SetValue(IsRefreshingProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsRefreshing.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsRefreshingProperty =
            BindableProperty.Create("IsRefreshing", typeof(bool), typeof(MenuPageViewModel), default(bool));

        #endregion

        public bool IsPresented { get; private set; }

        public MenuPageViewModel PreviousPage { get; set; }

        public string Title { get; }

        public AppStateGroupViewModel Group { get; }

        protected MenuPageViewModel(string title, AppStateGroupViewModel group)
        {
            this.Title = title;
            this.Group = group;
            this.Group.Add(this);

            this.IsRefreshing = false;
            this.IsRefreshAvailable = false;
            this.RefreshCommand = new Command(() => {
                this.OnRefresh();
            });
        }

        public virtual void OnEnter() { this.IsPresented = true; }

        public virtual void OnExit() { this.IsPresented = false; }

        public virtual void OnRefresh() { }
    }

    public class AppViewModel : BindableObject
    {
        #region string CurrentTitle 

        public string CurrentTitle
        {
            get { return (string)this.GetValue(CurrentTitleProperty); }
            set { this.SetValue(CurrentTitleProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentTitle.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentTitleProperty =
            BindableProperty.Create("CurrentTitle", typeof(string), typeof(AppViewModel), default(string));

        #endregion

        #region IEnumerable<object> CurrentVisiblePages 

        public IEnumerable<object> CurrentVisiblePages
        {
            get { return (IEnumerable<object>)this.GetValue(CurrentVisiblePagesProperty); }
            set { this.SetValue(CurrentVisiblePagesProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentVisiblePages.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentVisiblePagesProperty =
            BindableProperty.Create("CurrentVisiblePages", typeof(IEnumerable<object>), typeof(AppViewModel), default(IEnumerable<object>), propertyChanged: OnCurrentVisiblePagesChanged);

        #endregion

        private static void OnCurrentVisiblePagesChanged(BindableObject obj, object oldValue, object newValue)
        {
            if (obj is AppViewModel vm && newValue is IEnumerable<object> items)
            {
                vm.CurrentTitle = string.Join(", ", items.OfType<MenuPageViewModel>().Select(p => p.Title));
            }
        }

        public static AppViewModel Instance { get; private set; }

        public bool IsOnUwp { get; }

        #region double DesiredPageWidth 

        public double DesiredPageWidth
        {
            get { return (double)this.GetValue(DesiredPageWidthProperty); }
            set { this.SetValue(DesiredPageWidthProperty, value); }
        }

        // Using a BindableProperty as the backing store for DesiredPageWidth.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty DesiredPageWidthProperty =
            BindableProperty.Create("DesiredPageWidth", typeof(double), typeof(AppViewModel), default(double));

        #endregion

        #region AppStateViewModel CurrentStateModel 

        public AppStateViewModel CurrentStateModel
        {
            get { return (AppStateViewModel)this.GetValue(CurrentStateModelProperty); }
            set { this.SetValue(CurrentStateModelProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentStateModel. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentStateModelProperty =
            BindableProperty.Create("CurrentStateModel", typeof(AppStateViewModel), typeof(AppViewModel), default(AppStateViewModel));

        #endregion

        private readonly HomeAppViewModel _home;
        private readonly RootAppViewModel _root;
        private readonly MusicShareSvcApi _api = new MusicShareSvcApi("http://172.16.100.47:8181/mysvc", WebSvcMode.Xml);

        internal MusicShareSvcApi Api { get { return _api; } }

        #region bool HasError 

        public bool HasError
        {
            get { return (bool)this.GetValue(HasErrorProperty); }
            set { this.SetValue(HasErrorProperty, value); }
        }


        // Using a BindableProperty as the backing store for HasError. This enables animation, styling, binding, etc...
        public static readonly BindableProperty HasErrorProperty =
            BindableProperty.Create("HasError", typeof(bool), typeof(AppViewModel), default(bool));

        #endregion

        #region bool OperationInProgress 

        public bool OperationInProgress
        {
            get { return (bool)this.GetValue(OperationInProgressProperty); }
            set { this.SetValue(OperationInProgressProperty, value); }
        }

        // Using a BindableProperty as the backing store for OperationInProgress. This enables animation, styling, binding, etc...
        public static readonly BindableProperty OperationInProgressProperty =
            BindableProperty.Create("OperationInProgress", typeof(bool), typeof(AppViewModel), default(bool));

        #endregion

        public ObservableCollection<PopupEntry> Popups { get; } = new ObservableCollection<PopupEntry>();

        #region ProfileFootprintInfoType ProfileInfo 

        public ProfileFootprintInfoType ProfileInfo
        {
            get { return (ProfileFootprintInfoType)this.GetValue(ProfileInfoProperty); }
            set { this.SetValue(ProfileInfoProperty, value); }
        }

        // Using a BindableProperty as the backing store for ProfileInfo. This enables animation, styling, binding, etc...
        public static readonly BindableProperty ProfileInfoProperty =
            BindableProperty.Create("ProfileInfo", typeof(ProfileFootprintInfoType), typeof(AppViewModel), default(ProfileFootprintInfoType));

        #endregion

        public ICommand ClearErrorsCommand { get; }

        public AppViewModel()
        {
            Instance = this;
            this.IsOnUwp = DeviceInfo.Platform == DevicePlatform.UWP;

            AppDomain.CurrentDomain.FirstChanceException += (sender, ea) => {
                System.Diagnostics.Debug.Print(ea.Exception.ToString());
                System.Diagnostics.Debug.Print(ea.Exception.StackTrace);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, ea) => {
                var ex = (Exception)ea.ExceptionObject;
                System.Diagnostics.Debug.Print(ex.ToString());
                System.Diagnostics.Debug.Print(ex.StackTrace);
                try { _api.PushErrorReport(ex).Wait(); } catch { }
            };

            _home = new HomeAppViewModel();
            _root = new RootAppViewModel();

            this.CurrentStateModel = _home;
            this.CurrentStateModel.CurrentPage = _home.PlaybackPage;

            this.ClearErrorsCommand = new Command(this.ClearPopups);
        }

        public IActivity Activity { get; private set; }

        public void SetActivity(IActivity activity)
        {
            this.Activity = activity;
        }

        private async void OnLogin()
        {
            var footprint = await _api.GetProfileFootprint();
            this.ProfileInfo = footprint;

            _root.CurrentPage = _root.DefaultPage;
            this.CurrentStateModel = _root;

        }

        internal async void Login(string login, string password)
        {
            try
            {
                this.OperationInProgress = true;
                await _api.Login(login, password);
                this.OnLogin();
            }
            catch (WebApiException ex)
            {
                this.PostError(ex.Message);
            }
            finally
            {
                this.OperationInProgress = false;
            }
        }

        internal async void Logout()
        {
            try
            {
                this.OperationInProgress = true;
                await _api.Logout();

                _home.CurrentPage = _home.DefaultPage;
                _root.CurrentPage = _root.AboutPage;

                this.ProfileInfo = null;
                this.CurrentStateModel = _home;
            }
            catch (WebApiException ex)
            {
                this.PostError(ex.Message);
            }
            finally
            {
                this.OperationInProgress = false;
            }
        }

        internal async void Register(string login, string email, string password, string password2)
        {
            try
            {
                this.OperationInProgress = true;
                await _api.Register(login, email, password, password2);
                _home.CurrentPage = _home.LoginPage;
            }
            catch (WebApiException ex)
            {
                this.PostError(ex.Message);
            }
            finally
            {
                this.OperationInProgress = false;
            }

        }

        internal async Task<bool> Restore(string login, string email, string email2)
        {
            try
            {
                this.OperationInProgress = true;
                await _api.Restore(login, email, email2);
                return true;
            }
            catch (WebApiException ex)
            {
                this.PostError(ex.Message);
                return false;
            }
            finally
            {
                this.OperationInProgress = false;
            }
        }

        public async void ApplyAction(string actionName, string keyStr)
        {
            try
            {
                this.OperationInProgress = true;
                switch (actionName)
                {
                    case "activate":
                        _home.CurrentPage = _home.LoginPage;
                        this.CurrentStateModel = _home;
                        await _api.Activate(keyStr);
                        this.OnLogin();
                        break;
                    case "restore":
                        _home.CurrentPage = _home.RestorePage;
                        this.CurrentStateModel = _home;
                        await _api.RestoreAccess(keyStr);
                        this.OnLogin();
                        break;
                    default:
                        this.PostError($"Unable to perform unknown action '{actionName}'");
                        break;
                }
                //this.PostPopup(PopupEntrySeverity.Warning, $"Action '{actionName}' successed");
            }
            catch (WebApiException ex)
            {
                this.PostError(ex.Message);
            }
            finally
            {
                this.OperationInProgress = false;
            }
        }

        public void PostError(string message)
        {
            this.HasError = true;
            this.Popups.Add(new PopupEntry(message, this.CleanPopup));
        }

        public void PostPopup(PopupEntrySeverity severity, string message)
        {
            this.HasError = true;
            this.Popups.Add(new PopupEntry(severity, DateTime.Now, message, this.CleanPopup));
        }

        private void CleanPopup(PopupEntry entry)
        {
            this.Popups.Remove(entry);
            this.HasError = this.Popups.Count > 0;
        }

        private void ClearPopups()
        {
            this.Popups.Clear();
            this.HasError = false;
        }

        internal void UpdateSize(Size size)
        {
            if (size.Height > 480)
            {
                this.DesiredPageWidth = Math.Min(size.Width, size.Height * 3.0 / 4.0);
            }
            else
            {
                this.DesiredPageWidth = size.Width;
            }
        }
    }

    public enum PopupEntrySeverity
    {
        Info,
        Warning,
        Error
    }

    public class PopupEntry
    {
        public string Stamp { get; }
        public string Text { get; }
        public ICommand CloseCommand { get; }
        public PopupEntrySeverity Severity { get; }

        public PopupEntry(string text, Action<PopupEntry> closeAction)
            : this(PopupEntrySeverity.Error, DateTime.Now, text, closeAction) { }

        public PopupEntry(PopupEntrySeverity severity, DateTime stamp, string text, Action<PopupEntry> closeAction)
        {
            this.Severity = severity;
            this.Stamp = stamp.ToLongTimeString();
            this.Text = text;
            this.CloseCommand = new Command(() => closeAction(this));
        }
    }
}
