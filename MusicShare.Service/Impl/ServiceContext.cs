using MusicShare.Interaction.Standard;
using MusicShare.Intraction.Db;
using MusicShare.Service.Db;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Service
{
    class MusicShareServiceContext : IDisposable, IMusicShareServiceContext
    {
        private const string _sessionCookieName = "_mlsSessionId";

        private readonly DisposableList _disposables = new DisposableList();

        public MusicShareServiceConfiguration Configuration { get; }

        public string DbConnectionString { get; }
        public IMusicShareSessionsManager SessionsManager { get; }
        public ISecureRandom SecureRandom { get; }

        public DateTime UtcNow { get { return DateTime.UtcNow; } }

        public MusicShareServiceContext(MusicShareServiceConfiguration cfg)
        {
            this.Configuration = cfg;
            this.DbConnectionString = cfg.MakeDbConnectionString();

            this.Initialize();

            this.SessionsManager = new MusicShareSessionsManager(cfg.SessionTimeout);
            this.SecureRandom = _disposables.Add(new SecureRandom());

            var deliveryTimer = _disposables.Add(new TimerImpl());
        }

        private void Initialize()
        {
            using (var ctx = this.OpenLocalContext())
            {
                ctx.Db.Raw.CreateTables();
                //if (!ctx.Db.Raw.DatabaseExists())
                //    ctx.Db.Raw.CreateDatabase();
            }
        }

        public void SendMail(string targetEmail, string subject, string text)
        {
            // "FromName<FromLogin@host>"
            using (MailMessage mm = new MailMessage(this.Configuration.SmtpLogin, targetEmail))
            {
                mm.Subject = subject;
                mm.Body = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
 <head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <title>" + subject + @"</title>
</head>
<body>
" + text + @"
</body>
</html>";
                mm.IsBodyHtml = true;
                mm.Headers["Content-Type"] = "text/html; charset=utf-8";
                mm.BodyEncoding = Encoding.UTF8;

                using (SmtpClient sc = new SmtpClient(this.Configuration.SmtpServerHost, this.Configuration.SmtpServerPort))
                {
                    sc.PickupDirectoryLocation = this.Configuration.SmtpPickupDirectoryLocation;
                    sc.EnableSsl = this.Configuration.SmtpUseSsl;
                    sc.DeliveryMethod = this.Configuration.SmtpDeliveryMethod;
                    sc.UseDefaultCredentials = this.Configuration.SmtpUseDefaultCredentials;
                    sc.Credentials = new NetworkCredential(this.Configuration.SmtpLogin, this.Configuration.SmtpPassword);
                    sc.Send(mm);
                }
            }
        }

        public IBasicOperationContext OpenLocalContext()
        {
            return new BasicOperationContext(this);
        }

        public IMusicShareRequestContext OpenWebRequestContext()
        {
            return new MusicShareRequestContext(this, this.ResolveSession());
        }

        private IMusicShareSessionContext ResolveSession()
        {
            IMusicShareSessionContext session;
            Guid sessionId;

            if (WcfUtils.GetCookies().TryGetValue(_sessionCookieName, out string encodedSessionIdStr))
            {
                sessionId = Guid.Parse(encodedSessionIdStr.FromBase64());
                if (this.SessionsManager.TryGetSession(sessionId, out session))
                    session.Renew();
                else
                    session = null;
            }
            else
            {
                session = null;
            }

            if (session == null)
            {
                session = this.SessionsManager.CreateSession();

                WcfUtils.AddResponseCookie(_sessionCookieName, session.Id.ToString().ToBase64(), DateTime.UtcNow.AddYears(10), path: "/", httpOnly: true);
            }

            return session;
        }

        public IMusicShareDbContext OpenDb()
        {
            var cnn = new SqlConnection(this.DbConnectionString);
            cnn.InfoMessage += (sneder, ea) => {
                System.Diagnostics.Debug.Print(ea.Source + ": " + ea.Message);
                ea.Errors.OfType<SqlError>().ToList().ForEach(e => {
                    System.Diagnostics.Debug.Print(e.Source + " (" + e.Procedure + ") : " + e.Message);
                });
            };
            cnn.Open();

            var ctx = new DbContext(cnn);
            ctx.Log = new DebugTextWriter();
            return new MusicShareDbContext(ctx);
        }

        public void Dispose()
        {
            _disposables.SafeDispose();
        }

        public byte[] DownloadContent(string resourceUrl)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            var data = wc.DownloadData(resourceUrl);
            return data;
        }
    }

    public class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter()
            : base(new DebugOutStream(), Encoding.Unicode, 1024)
        {
            this.AutoFlush = true;
        }

        sealed class DebugOutStream : Stream
        {
            public override void Write(byte[] buffer, int offset, int count)
            {
                System.Diagnostics.Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
                this.Flush();
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override void Flush() => System.Diagnostics.Debug.Flush();

            public override long Length => throw bad_op;
            public override int Read(byte[] buffer, int offset, int count) => throw bad_op;
            public override long Seek(long offset, SeekOrigin origin) => throw bad_op;
            public override void SetLength(long value) => throw bad_op;
            public override long Position
            {
                get => throw bad_op;
                set => throw bad_op;
            }

            static InvalidOperationException bad_op => new InvalidOperationException();
        };
    }
}
