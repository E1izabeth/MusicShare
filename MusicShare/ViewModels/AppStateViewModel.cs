using MusicShare.ViewModels.Home;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
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

        #region MenuPageViewModel CurrentPage 

        public MenuPageViewModel CurrentPage
        {
            get { return (MenuPageViewModel)this.GetValue(CurrentPageProperty); }
            set { this.SetValue(CurrentPageProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentPage. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentPageProperty =
            BindableProperty.Create("CurrentPage", typeof(MenuPageViewModel), typeof(AppStateViewModel), default(MenuPageViewModel),
                                    propertyChanged: OnCurrentPageChanged);

        #endregion

        static void OnCurrentPageChanged(BindableObject bindable, object oldValue, object newValue)
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
                newPage.OnEnter();
        }
    }

    public class HomeAppViewModel : AppStateViewModel
    {
        // public override bool IsLoggedIn { get { return false; } }

        public MenuPageViewModel LoginPage { get; }
        public MenuPageViewModel RegisterPage { get; }
        public MenuPageViewModel RestorePage { get; }

        public MenuPageViewModel DefaultPage { get; }

        public HomeAppViewModel(AppViewModel app)
        {
            _menuPages.AddRange(new MenuPageViewModel[] {
                this.DefaultPage = this.LoginPage = new LoginViewModel(app),
                this.RegisterPage = new RegisterViewModel(app),
                this.RestorePage = new RestoreViewModel(app),
                new ConnectivityViewModel(app),
                new PlaybackViewModel(app),
                new AboutViewModel(app),
            });

            this.CurrentPage = _menuPages[0];
        }
    }

    public class RootAppViewModel : AppStateViewModel
    {
        // public override bool IsLoggedIn { get { return true; } }

        public MenuPageViewModel DefaultPage { get; }
        public AboutViewModel AboutPage { get; }

        public RootAppViewModel(AppViewModel app)
        {
            _menuPages.AddRange(new MenuPageViewModel[] {
                this.DefaultPage = this.AboutPage,
                new ProfileViewModel(app),
                this.AboutPage = new AboutViewModel(app),
                new LogoutViewModel(app)
            });

            this.CurrentPage = this.AboutPage;
            // this.IsLoggedIn = true;
        }
    }
}
