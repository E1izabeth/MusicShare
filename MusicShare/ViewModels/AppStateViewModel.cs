using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MusicShare.ViewModels.Home;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public abstract class AppStateViewModel : BindableObject
    {
        //#region bool IsLoggedIn 

        //public bool IsLoggedIn
        //{
        //    get { return (bool)this.GetValue(IsLoggedInProperty); }
        //    set { this.SetValue(IsLoggedInProperty, value); }
        //}

        //// Using a BindableProperty as the backing store for IsLoggedIn. This enables animation, styling, binding, etc...
        //public static readonly BindableProperty IsLoggedInProperty =
        //    BindableProperty.Create("IsLoggedIn", typeof(bool), typeof(AppStateViewModel), default(bool));

        //#endregion

        #region AppStateGroupViewModel CurrentGroup 

        public AppStateGroupViewModel CurrentGroup
        {
            get { return (AppStateGroupViewModel)this.GetValue(CurrentGroupProperty); }
            set { this.SetValue(CurrentGroupProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentGroup.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentGroupProperty =
            BindableProperty.Create("CurrentGroup", typeof(AppStateGroupViewModel), typeof(AppStateViewModel), default(AppStateGroupViewModel), propertyChanged: OnCurrentGroupChanged);

        #endregion

        private static void OnCurrentGroupChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is AppStateViewModel s)
            {
                s.CurrentPage = null;
            }
        }

        #region MenuPageViewModel CurrentPage 

        public MenuPageViewModel CurrentPage
        {
            get { return (MenuPageViewModel)this.GetValue(CurrentPageProperty); }
            set { this.SetValue(CurrentPageProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentPage. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentPageProperty =
            BindableProperty.Create("CurrentPage", typeof(MenuPageViewModel), typeof(AppStateViewModel), default(MenuPageViewModel), propertyChanged: OnCurrentPageChanged);

        #endregion

        private static void OnCurrentPageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is AppStateViewModel s)
                s.OnCurrentPageChanged(oldValue, newValue);
        }

        public ReadOnlyCollection<MenuPageViewModel> MenuPages { get; }

        protected List<MenuPageViewModel> _menuPages = new List<MenuPageViewModel>();

        public AppStateViewModel()
        {
            this.MenuPages = new ReadOnlyCollection<MenuPageViewModel>(_menuPages);
        }

        protected virtual void OnCurrentPageChanged(object oldValue, object newValue)
        {
            if (oldValue is MenuPageViewModel oldPage)
                oldPage.OnExit();

            if (newValue is MenuPageViewModel newPage)
            {
                this.CurrentGroup = newPage.Group;
                newPage.OnEnter();
            }
        }
    }

    public class AppStateGroupViewModel : BindableObject
    {
        #region ObservableCollection<MenuPageViewModel> SiblingPages 

        public ObservableCollection<MenuPageViewModel> SiblingPages
        {
            get { return (ObservableCollection<MenuPageViewModel>)this.GetValue(SiblingPagesProperty); }
            set { this.SetValue(SiblingPagesProperty, value); }
        }

        // Using a BindableProperty as the backing store for SiblingPages.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty SiblingPagesProperty =
            BindableProperty.Create("SiblingPages", typeof(ObservableCollection<MenuPageViewModel>), typeof(AppStateGroupViewModel), default(ObservableCollection<MenuPageViewModel>));

        #endregion

        public AppStateGroupViewModel()
        {
            this.SiblingPages = new ObservableCollection<MenuPageViewModel>();
        }

        internal void Add(MenuPageViewModel page)
        {
            this.SiblingPages.Add(page);
        }
    }

    public class HomeAppViewModel : AppStateViewModel
    {
        // public override bool IsLoggedIn { get { return false; } }

        public ConnectivityViewModel ConnectivityPage { get; }
        public PlaybackViewModel PlaybackPage { get; }

        public MenuPageViewModel LoginPage { get; }
        public MenuPageViewModel RegisterPage { get; }
        public MenuPageViewModel RestorePage { get; }

        public MenuPageViewModel DefaultPage { get; }

        public HomeAppViewModel()
        {
            var g1 = new AppStateGroupViewModel();
            var g2 = new AppStateGroupViewModel();
            var g3 = new AppStateGroupViewModel();

            _menuPages.AddRange(new MenuPageViewModel[] {
                this.ConnectivityPage = new ConnectivityViewModel(g1),
                this.PlaybackPage = new PlaybackViewModel(g1),
                this.LoginPage = new LoginViewModel(g2),
                this.RegisterPage = new RegisterViewModel(g2),
                this.RestorePage = new RestoreViewModel(g2),
                new AboutViewModel(g3),
            });

            this.CurrentPage = _menuPages[1];
        }
    }

    public class RootAppViewModel : AppStateViewModel
    {
        // public override bool IsLoggedIn { get { return true; } }

        public MenuPageViewModel DefaultPage { get; }
        public AboutViewModel AboutPage { get; }

        public RootAppViewModel()
        {
            var g1 = new AppStateGroupViewModel();

            _menuPages.AddRange(new MenuPageViewModel[] {
                new ProfileViewModel(g1),
                this.AboutPage = new AboutViewModel(g1),
                new LogoutViewModel(g1),
            });

            this.CurrentPage = this.AboutPage;
            // this.IsLoggedIn = true;
        }
    }
}
