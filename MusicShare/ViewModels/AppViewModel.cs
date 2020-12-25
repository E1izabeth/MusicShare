using BruTile.Tms;
using MusicShare.Models;
using MusicShare.ViewModels.Home;
using MusicShare.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public abstract class MenuPageViewModel : BindableObject
    {
        public bool IsPresented { get; private set; }

        public MenuPageViewModel PreviousPage { get; set; }

        public string Title { get; }

        protected MenuPageViewModel(string title)
        {
            this.Title = title;
        }

        public virtual void OnEnter() { this.IsPresented = true; }

        public virtual void OnExit() { this.IsPresented = false; }
    }

    public class AppViewModel : BindableObject
    {
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

        readonly HomeAppViewModel _home;
        readonly RootAppViewModel _root;

        readonly MusicShareSvcApi _api = new MusicShareSvcApi("http://172.16.100.47:8181/mysvc", WebSvcMode.Xml);

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

        internal ConnectivityViewModel ConnectivityViewModel { get;private set; }
        internal PlaybackViewModel PlaybackViewModel { get;private set; }

        public AppViewModel()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, ea) =>
            {
                System.Diagnostics.Debug.Print(ea.Exception.ToString());
                System.Diagnostics.Debug.Print(ea.Exception.StackTrace);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, ea) =>
            {
                var ex = (Exception)ea.ExceptionObject;
                System.Diagnostics.Debug.Print(ex.ToString());
                System.Diagnostics.Debug.Print(ex.StackTrace);
                try { _api.PushErrorReport(ex).Wait(); } catch { }
            };

            this.ConnectivityViewModel = new ConnectivityViewModel(this);
            this.PlaybackViewModel = new PlaybackViewModel(this);

            _home = new HomeAppViewModel(this);
            _root = new RootAppViewModel(this);

            this.CurrentStateModel = _home;

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
