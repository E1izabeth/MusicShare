using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using MusicShare.Service.Db;
using MusicShare.Interaction;
using MusicShare.Service.Util;
using System.Security.Policy;
using System.Data.Linq;
using System.Threading;
using System.Xml.Serialization;
using MusicShare.Intraction.Db;

namespace MusicShare.Service.Impl
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    [XmlSerializerFormat]
    class MusicShareServiceImpl : IMusicShareSvc
    {
        readonly IMusicShareServiceContext _ctx;

        public MusicShareServiceImpl(IMusicShareServiceContext ctx)
        {
            _ctx = ctx;
        }

        #region profile management

        public void Register(RegisterSpecType registerSpec)
        {
            if (registerSpec.Login.IsEmpty())
                throw new ApplicationException("Login cannot be empty");

            if (registerSpec.Password.IsEmpty() || registerSpec.Password.Length < 10)
                throw new ApplicationException("Password should be of length >10 characters");

            using (var ctx = _ctx.OpenWebRequestContext())
            {
                var loginKey = registerSpec.Login.ToLower();
                if (ctx.Db.Users.FindUserByLoginKey(loginKey) != null)
                {
                    throw new ApplicationException("User " + registerSpec.Login + " already exists");
                }
                else
                {
                    var salt = Convert.ToBase64String(_ctx.SecureRandom.GenerateRandomBytes(64));

                    var user = new DbUserInfo()
                    {
                        Activated = false,
                        HashSalt = salt,
                        Email = registerSpec.Email,
                        IsDeleted = false,
                        RegistrationStamp = DateTime.UtcNow,
                        Login = registerSpec.Login,
                        LoginKey = registerSpec.Login.ToLower(),
                        PasswordHash = registerSpec.Password.ComputeSha256Hash(salt),
                        LastLoginStamp = SqlDateTime.MinValue.Value,
                        LastTokenStamp = SqlDateTime.MinValue.Value
                    };
                    ctx.Db.Users.AddUser(user);
                    ctx.Db.SubmitChanges();

                    this.RequestActivationImpl(ctx, user, registerSpec.Email);
                    ctx.Db.SubmitChanges();
                }
            }
        }

        public void RequestActivation(RequestActivationSpecType spec)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.ValidateAuthorized(false);
                this.RequestActivationImpl(ctx, ctx.Db.Users.GetUserById(ctx.Session.UserId), spec.Email);
                ctx.Db.SubmitChanges();
            }
        }

        private string MakeToken()
        {
            return new[] { "=", "/", "+" }.Aggregate(Convert.ToBase64String(_ctx.SecureRandom.GenerateRandomBytes(64)), (s, c) => s.Replace(c, string.Empty));
        }

        private void RequestActivationImpl(IMusicShareRequestContext ctx, DbUserInfo user, string email)
        {
            if (user.Activated)
                throw new ApplicationException("Already activated");

            var activationToken = this.MakeToken();

            user.LastToken = activationToken;
            user.LastTokenStamp = DateTime.UtcNow;
            user.LastTokenKind = DbUserTokenKind.Activation;

            _ctx.SendMail(
                email, "Registration activation",
                $"To confirm your registration follow this link: " + this.MakeSeriveLink(ctx, "/profile?action=activate&key=" + activationToken)
            );
        }

        string MakeSeriveLink(IMusicShareRequestContext ctx, string relLink)
        {
            // /profile?action=activate&key={key}

            var urlBuilder = _ctx.Configuration.GetServiceUrl();
            var pair = ctx.RequestHostName.Split(new[] { ':' }, 2);
            urlBuilder.Host = pair[0];
            if (pair.Length > 1 && ushort.TryParse(pair[1], out var port))
                urlBuilder.Port = port;

            var linkUri = new Uri(urlBuilder.Uri, relLink.TrimStart('/'));
            return @"<a href=""" + linkUri + @""">" + linkUri + "</a>";
        }

        public void RequestAccess(ResetPasswordSpecType spec)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                var loginKey = spec.Login.ToLower();

                var user = ctx.Db.Users.FindUserByLoginKey(loginKey);
                if (user != null && user.Email == spec.Email && !user.IsDeleted)
                {
                    var accessRestoreToken = this.MakeToken();

                    user.LastToken = accessRestoreToken;
                    user.LastTokenStamp = DateTime.UtcNow;
                    user.LastTokenKind = DbUserTokenKind.AccessRestore;
                    ctx.Db.SubmitChanges();

                    _ctx.SendMail(
                        spec.Email, "Access restore",
                        $"To regain access to your profile follow this link: " + this.MakeSeriveLink(ctx, "/profile?action=restore&key=" + accessRestoreToken)
                    );
                }
                else
                {
                    throw new ApplicationException("User not found or incorrect email");
                }
            }
        }

        public OkType Activate(string key)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                var user = ctx.Db.Users.FindUserByTokenKey(key);
                if (user != null && user.LastToken != null && user.LastTokenKind == DbUserTokenKind.Activation)
                {
                    if (user.Activated)
                        throw new ApplicationException("Already activated");

                    if (user.LastTokenStamp + _ctx.Configuration.TokenTimeout >= DateTime.UtcNow)
                    {
                        user.LastLoginStamp = DateTime.UtcNow;
                        user.LastToken = null;
                        user.Activated = true;
                        ctx.Db.SubmitChanges();
                        ctx.Session.SetUserContext(user);
                    }
                    else
                    {
                        throw new ApplicationException("Acivation token expired");
                    }
                }
                else
                {
                    throw new ApplicationException("Invalid activation token");
                }
            }

            return new OkType();
        }

        public OkType RestoreAccess(string key)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                var user = ctx.Db.Users.FindUserByTokenKey(key);
                if (user != null && user.LastToken != null && user.LastTokenKind == DbUserTokenKind.AccessRestore)
                {
                    if (user.LastTokenStamp + _ctx.Configuration.TokenTimeout >= DateTime.UtcNow)
                    {
                        user.LastLoginStamp = DateTime.UtcNow;
                        user.LastToken = null;
                        ctx.Db.SubmitChanges();
                        ctx.Session.SetUserContext(user);
                    }
                    else
                    {
                        throw new ApplicationException("Acivation token expired");
                    }
                }
                else
                {
                    throw new ApplicationException("Invalid activation token");
                }
            }

            return new OkType();
        }

        public void SetEmail(ChangeEmailSpecType spec)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.ValidateAuthorized(false);

                var user = ctx.Db.Users.GetUserById(ctx.Session.UserId);
                if (user.Email == spec.OldEmail &&
                    user.PasswordHash == spec.Password.ComputeSha256Hash(user.HashSalt))
                {
                    user.Email = spec.NewEmail;
                    ctx.Db.SubmitChanges();
                }
                else
                {
                    throw new ApplicationException("Invalid old email or password");
                }
            }
        }

        public void SetPassword(ChangePasswordSpecType spec)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.ValidateAuthorized(false);

                var user = ctx.Db.Users.GetUserById(ctx.Session.UserId);
                if (user.Email == spec.Email)
                // user.PasswordHash == spec.OldPassword.ComputeSha256Hash(user.HashSalt))
                {
                    user.PasswordHash = spec.NewPassword.ComputeSha256Hash(user.HashSalt);
                    ctx.Db.SubmitChanges();

                    _ctx.SendMail(spec.Email, "Password was changed", "Dear " + user.Login + ", your password was changed!");
                }
                else
                {
                    throw new ApplicationException("Invalid old email");
                }
            }
        }

        public void Login(LoginSpecType loginSpec)
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                var loginKey = loginSpec.Login;
                var user = ctx.Db.Users.FindUserByLoginKey(loginKey);
                if (user != null && user.PasswordHash == loginSpec.Password.ComputeSha256Hash(user.HashSalt) && !user.IsDeleted)
                {
                    user.LastLoginStamp = DateTime.UtcNow;
                    ctx.Db.SubmitChanges();

                    ctx.Session.SetUserContext(user);
                }
                else
                {
                    throw new ApplicationException("Invalid credentials");
                }
            }
        }

        public void Logout()
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.Session.SetUserContext(null);
            }
        }

        public void DeleteProfile()
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.ValidateAuthorized();

                var user = ctx.Db.Users.GetUserById(ctx.Session.UserId);
                user.IsDeleted = true;
                user.LastToken = null;

                _ctx.SessionsManager.DropUserSessions(user.Id);
                ctx.Db.SubmitChanges();
            }
        }

        public ProfileFootprintInfoType GetProfileFootprint()
        {
            using (var ctx = _ctx.OpenWebRequestContext())
            {
                ctx.ValidateAuthorized(false);

                var user = ctx.Db.Users.GetUserById(ctx.Session.UserId);

                var parts = user.Email.Split('@');
                var leading = parts[0].Substring(0, Math.Min(2, parts[0].Length));
                var suffixDotPos = parts[1].LastIndexOf('.');
                var ending = suffixDotPos > 0 ? parts[1].Substring(suffixDotPos) : parts[1].Substring(parts[1].Length - Math.Min(2, parts[1].Length));
                var emailFootprint = leading + "***@***" + ending;

                return new ProfileFootprintInfoType()
                {
                    Login = user.Login,
                    EmailFootprint = emailFootprint,
                    IsActivated = user.Activated
                };
            }
        }

        #endregion


   


        public void PushErrorReport(ErrorInfoType errorInfo)
        {
            System.Diagnostics.Debug.Print("Remote error info received: " + Environment.NewLine + errorInfo.FormatErrorInfo());
        }

    }
}

