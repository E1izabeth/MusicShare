using MusicShare.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace MusicShare.ViewModels.Home
{
    class ProfileViewModel : MenuPageViewModel
    {
        #region string OldEmail 

        public string OldEmail
        {
            get { return (string)this.GetValue(OldEmailProperty); }
            set { this.SetValue(OldEmailProperty, value); }
        }

        // Using a BindableProperty as the backing store for OldEmail. This enables animation, styling, binding, etc...
        public static readonly BindableProperty OldEmailProperty =
            BindableProperty.Create("OldEmail", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string NewEmail 

        public string NewEmail
        {
            get { return (string)this.GetValue(NewEmailProperty); }
            set { this.SetValue(NewEmailProperty, value); }
        }

        // Using a BindableProperty as the backing store for NewEmail. This enables animation, styling, binding, etc...
        public static readonly BindableProperty NewEmailProperty =
            BindableProperty.Create("NewEmail", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string NewEmail2 

        public string NewEmail2
        {
            get { return (string)this.GetValue(NewEmail2Property); }
            set { this.SetValue(NewEmail2Property, value); }
        }

        // Using a BindableProperty as the backing store for NewEmail2. This enables animation, styling, binding, etc...
        public static readonly BindableProperty NewEmail2Property =
            BindableProperty.Create("NewEmail2", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string CurrentPassword 

        public string CurrentPassword
        {
            get { return (string)this.GetValue(CurrentPasswordProperty); }
            set { this.SetValue(CurrentPasswordProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentPassword. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentPasswordProperty =
            BindableProperty.Create("CurrentPassword", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string NewPassword 

        public string NewPassword
        {
            get { return (string)this.GetValue(NewPasswordProperty); }
            set { this.SetValue(NewPasswordProperty, value); }
        }

        // Using a BindableProperty as the backing store for NewPassword. This enables animation, styling, binding, etc...
        public static readonly BindableProperty NewPasswordProperty =
            BindableProperty.Create("NewPassword", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string NewPassword2 

        public string NewPassword2
        {
            get { return (string)this.GetValue(NewPassword2Property); }
            set { this.SetValue(NewPassword2Property, value); }
        }

        // Using a BindableProperty as the backing store for NewPassword2. This enables animation, styling, binding, etc...
        public static readonly BindableProperty NewPassword2Property =
            BindableProperty.Create("NewPassword2", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string CurrentEmail 

        public string CurrentEmail
        {
            get { return (string)this.GetValue(CurrentEmailProperty); }
            set { this.SetValue(CurrentEmailProperty, value); }
        }

        // Using a BindableProperty as the backing store for CurrentEmail. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CurrentEmailProperty =
            BindableProperty.Create("CurrentEmail", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region string ActivationEmail 

        public string ActivationEmail
        {
            get { return (string)this.GetValue(ActivationEmailProperty); }
            set { this.SetValue(ActivationEmailProperty, value); }
        }

        // Using a BindableProperty as the backing store for ActivationEmail. This enables animation, styling, binding, etc...
        public static readonly BindableProperty ActivationEmailProperty =
            BindableProperty.Create("ActivationEmail", typeof(string), typeof(ProfileViewModel), default(string));

        #endregion

        #region bool IsEmailAreaExpanded 

        public bool IsEmailAreaExpanded
        {
            get { return (bool)this.GetValue(IsEmailAreaExpandedProperty); }
            set { this.SetValue(IsEmailAreaExpandedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsEmailAreaExpanded. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsEmailAreaExpandedProperty =
            BindableProperty.Create("IsEmailAreaExpanded", typeof(bool), typeof(ProfileViewModel), default(bool));

        #endregion

        #region bool IsPasswordAreaExpanded 

        public bool IsPasswordAreaExpanded
        {
            get { return (bool)this.GetValue(IsPasswordAreaExpandedProperty); }
            set { this.SetValue(IsPasswordAreaExpandedProperty, value); }
        }

        // Using a BindableProperty as the backing store for IsPasswordAreaExpanded. This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsPasswordAreaExpandedProperty =
            BindableProperty.Create("IsPasswordAreaExpanded", typeof(bool), typeof(ProfileViewModel), default(bool));

        #endregion

        public ICommand ChangeEmailCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand ActivateProfileCommand { get; }

        public AppViewModel App { get; }

        public ProfileViewModel(AppViewModel app)
            : base("Profile")
        {
            this.App = app;
            this.ChangeEmailCommand = new Command(async () =>
            {
                if (this.NewEmail != this.NewEmail2)
                {
                    app.PostError("Emails are not match");
                }
                else
                {
                    try
                    {
                        app.OperationInProgress = true;
                        await app.Api.SetEmail(this.CurrentPassword, this.OldEmail, this.NewEmail);
                        app.PostPopup(PopupEntrySeverity.Info, "Email succesfully changed!");
                        this.IsEmailAreaExpanded = false;
                        this.ResetProps();
                    }
                    catch (WebApiException ex)
                    {
                        app.PostError(ex.Message);
                    }
                    finally
                    {
                        app.OperationInProgress = false;
                    }
                }
            });

            this.ChangePasswordCommand = new Command(async () =>
            {
                if (this.NewPassword != this.NewPassword2)
                {
                    app.PostError("Passwords are not match");
                }
                else
                {
                    try
                    {
                        app.OperationInProgress = true;
                        await app.Api.SetPassword(this.NewPassword, this.CurrentEmail);
                        app.PostPopup(PopupEntrySeverity.Info, "Password succesfully changed!");
                        this.IsPasswordAreaExpanded = false;
                        this.ResetProps();
                    }
                    catch (WebApiException ex)
                    {
                        app.PostError(ex.Message);
                    }
                    finally
                    {
                        app.OperationInProgress = false;
                    }
                }
            });

            this.ActivateProfileCommand = new Command(async () =>
            {
                try
                {
                    app.OperationInProgress = true;
                    await app.Api.RequestActivation(this.ActivationEmail);
                    app.PostPopup(PopupEntrySeverity.Info, "Activation link sent to your email!");
                }
                catch (WebApiException ex)
                {
                    app.PostError(ex.Message);
                }
                finally
                {
                    app.OperationInProgress = false;
                }
            });
        }

        private void ResetProps()
        {
            this.OldEmail = "";
            this.NewEmail = "";
            this.NewEmail2 = "";
            this.CurrentPassword = "";
            this.NewPassword = "";
            this.NewPassword2 = "";
            this.CurrentEmail = "";
        }
    }
}
