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

        public LoginViewModel(AppViewModel app)
            : base("Login")
        {
            this.LoginCommand = new Command(async () => app.Login(this.Login, this.Password));
        }
    }

    class RegisterViewModel : MenuPageViewModel
    {
        public string Login { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }

        public ICommand RegisterCommand { get; }

        public RegisterViewModel(AppViewModel app)
            : base("Register")
        {
            this.RegisterCommand = new Command(async () => app.Register(this.Login, this.Email, this.Password, this.Password2));
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

        public RestoreViewModel(AppViewModel app)
            : base("Restore")
        {
            this.ActionButtonEnabled = true;
            this.RestoreCommand = new Command(async () =>
            {
                var successed = await app.Restore(this.Login, this.Email, this.Email2);
                this.ActionButtonEnabled = !successed;
            });
        }
    }

    class LogoutViewModel : MenuPageViewModel
    {
        public ICommand LogoutCommand { get; }

        public LogoutViewModel(AppViewModel app)
            : base("Logout")
        {
            this.LogoutCommand = new Command(async () => { app.Logout();  }) ;
        }
    }
}
