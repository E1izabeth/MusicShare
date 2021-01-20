using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace MusicShare.ViewModels.Home
{

    class LoginViewModel : MenuPageViewModel
    {
        public string Login { get; set; }
        public string Password { get; set; }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AppStateGroupViewModel group)
            : base("Login", group)
        {
            this.LoginCommand = new Command(async () => this.App.Login(this.Login, this.Password));
        }
    }

    class RegisterViewModel : MenuPageViewModel
    {
        public string Login { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }

        public ICommand RegisterCommand { get; }

        public RegisterViewModel(AppStateGroupViewModel group)
            : base("Register", group)
        {
            this.RegisterCommand = new Command(async () => this.App.Register(this.Login, this.Email, this.Password, this.Password2));
        }
    }

    class RestoreViewModel : MenuPageViewModel
    {
        #region bool ActionButtonEnabled 

        public bool ActionButtonEnabled
        {
            get { return (bool)this.GetValue(ActionButtonEnabledProperty); }
            set { this.SetValue(ActionButtonEnabledProperty, value); }
        }

        // Using a BindableProperty as the backing store for ActionButtonEnabled. This enables animation, styling, binding, etc...
        public static readonly BindableProperty ActionButtonEnabledProperty =
            BindableProperty.Create("ActionButtonEnabled", typeof(bool), typeof(RestoreViewModel), default(bool));

        #endregion

        public string Login { get; set; }
        public string Email { get; set; }
        public string Email2 { get; set; }

        public ICommand RestoreCommand { get; }

        public RestoreViewModel(AppStateGroupViewModel group)
            : base("Restore", group)
        {
            this.ActionButtonEnabled = true;
            this.RestoreCommand = new Command(async () =>
            {
                var successed = await this.App.Restore(this.Login, this.Email, this.Email2);
                this.ActionButtonEnabled = !successed;
            });
        }
    }

    class LogoutViewModel : MenuPageViewModel
    {
        public ICommand LogoutCommand { get; }

        public LogoutViewModel(AppStateGroupViewModel group)
            : base("Logout", group)
        {
            this.LogoutCommand = new Command(async () => { this.App.Logout();  }) ;
        }
    }
}
