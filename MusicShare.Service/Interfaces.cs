using MusicShare.Intraction.Db;
using MusicShare.Service.Db;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Service
{
    interface IMusicShareServiceContext
    {
        MusicShareServiceConfiguration Configuration { get; }

        string DbConnectionString { get; }
        IMusicShareSessionsManager SessionsManager { get; }
        ISecureRandom SecureRandom { get; }

        IMusicShareDbContext OpenDb();
        void SendMail(string targetEmail, string subject, string text);
        byte[] DownloadContent(string resourceUrl);

        IBasicOperationContext OpenLocalContext();
        IMusicShareRequestContext OpenWebRequestContext();

        DateTime UtcNow { get; }
    }

    interface IMusicShareSessionsManager : IDisposable
    {
        TimeSpan SessionCleanupTimeout { get; set; }
        
        IMusicShareSessionContext CreateSession();
        
        IMusicShareSessionContext GetSession(Guid id);
        bool TryGetSession(Guid sessionId, out IMusicShareSessionContext session);
     
        void DeleteSession(Guid sessionId);
        void DropUserSessions(long userId);
        void CleanupSessions();
    }

    interface ISecureRandom : IDisposable
    {
        int Next(int minValue, int maxExclusiveValue);
        byte[] GenerateRandomBytes(int bytesNumber);
    }

    interface IBasicOperationContext : IDisposable
    {
        IMusicShareDbContext Db { get; }
    }

    interface IMusicShareRequestContext : IBasicOperationContext
    {
        string RequestHostName { get; }
        IMusicShareSessionContext Session { get; }
        void ValidateAuthorized();
        void ValidateAuthorized(bool requireActivated = true);
    }

    interface IMusicShareSessionContext
    {
        Guid Id { get; }
        DateTime LastActivity { get; }
        long UserId { get; }
        bool IsActivated { get; }

        event Action<long> OnUserContextChanging;

        void Renew();
        void SetUserContext(DbUserInfo user);
    }
}
