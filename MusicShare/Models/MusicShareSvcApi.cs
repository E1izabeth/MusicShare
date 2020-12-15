using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace MusicShare.Models
{
    class MusicShareSvcApi
    {
        readonly WebApiHelper _helper;

        public MusicShareSvcApi(string rootUrl, WebSvcMode mode)
        {
            _helper = new WebApiHelper(rootUrl, mode);
        }

        public async Task Login(string login, string password)
        {
            await _helper.Post("/profile?action=login", new LoginSpecType() { Login = login, Password = password });
        }

        public async Task Register(string login, string email, string password, string password2)
        {
            if (password != password2)
                throw new WebApiException("Passwords are not matched");
            else if (!Regex.Match(email, @"(\S)*@(\S)*.(\S)*").Success)
            {
                throw new WebApiException("Invalid email");
            }

            await _helper.Post("/profile?action=register", new RegisterSpecType() { Login = login, Email = email, Password = password });
        }

        public async Task Logout()
        {
            await _helper.Post("/profile?action=logout");
        }

        internal async Task Restore(string login, string email, string email2)
        {
            if (email != email2)
                throw new WebApiException("Emails are not matched");
            else if (!Regex.Match(email, @"(\S)*@(\S)*.(\S)*").Success)
            {
                throw new WebApiException("Invalid email");
            }

            await _helper.Post("/profile?action=restore", new ResetPasswordSpecType() { Login = login, Email = email });
        }

        internal async Task<OkType> Activate(string key)
        {
            return await _helper.Get<OkType>("/profile?action=activate&key=" + key);
        }

        internal async Task<OkType> RestoreAccess(string key)
        {
            return await _helper.Get<OkType>("/profile?action=restore&key=" + key);
        }

        internal async Task<ProfileFootprintInfoType> GetProfileFootprint()
        {
            return await _helper.Get<ProfileFootprintInfoType>("/profile");
        }

        internal async Task RequestActivation(string oldEmail)
        {
            await _helper.Post("/profile?action=activate",
                new RequestActivationSpecType() { Email = oldEmail }
            );
        }

        internal async Task SetEmail(string password, string oldEmail, string newEmail)
        {
            await _helper.Post("/profile?action=set-email",
                new ChangeEmailSpecType() { Password = password, NewEmail = newEmail, OldEmail = oldEmail }
            );
        }

        internal async Task SetPassword(string newPassword, string email)
        {
            await _helper.Post("/profile?action=set-password",
                new ChangePasswordSpecType() { Email = email, NewPassword = newPassword }
            );
        }

        internal async Task<StampInfoType> WaitForNotify(long orderId, DateTime from, TimeSpan timeout)
        {
            return await _helper.Get<StampInfoType>($"/notify?timeout={timeout.TotalSeconds}&from={from.Ticks}&order={orderId}");
        }

        internal async Task PushErrorReport(Exception ex)
        {
            await _helper.Post("/error-report", ex.MakeErrorInfo());
        }
    }
}
