using MusicShare.Interaction;
using MusicShare.Interaction.Standard;
using MusicShare.Intraction.Db;
using MusicShare.Service.Db;
using MusicShare.Service.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace MusicShare.Service.Impl
{
    class BasicOperationContext : IDisposable, IBasicOperationContext
    {
        protected readonly DisposableList _disposables = new DisposableList();

        private readonly IMusicShareServiceContext _svcCtx;

        IMusicShareDbContext _dbContext = null;
        public IMusicShareDbContext Db { get { return _dbContext ?? (_dbContext = this.OpenDb()); } }

        // private TransactionScope _transaction = null;

        public BasicOperationContext(IMusicShareServiceContext svcCtx)
        {
            _svcCtx = svcCtx;
        }

        private IMusicShareDbContext OpenDb()
        {
            var ctx = _svcCtx.OpenDb();
            _disposables.Add(ctx.Raw.Connection);
            _disposables.Add(ctx.Raw);
            //_transaction = _disposables.Add(new TransactionScope(
            //    TransactionScopeOption.Required,
            //    new TransactionOptions
            //    {
            //        IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
            //    }
            //));
            return ctx;
        }

        public void Dispose()
        {
            //if (_transaction != null)
            //    _transaction.Complete();

            _disposables.SafeDispose();
        }
    }

    class MusicShareRequestContext : BasicOperationContext, IMusicShareRequestContext
    {
        public string RequestHostName { get; private set; }
        public IMusicShareSessionContext Session { get; private set; }

        public MusicShareRequestContext(IMusicShareServiceContext svcCtx, IMusicShareSessionContext session)
            : base(svcCtx)
        {
            this.Session = session;
            
            this.RequestHostName = WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.Host];
        }

        public void ValidateAuthorized()
        {
            this.ValidateAuthorized(true);
        }

        public void ValidateAuthorized(bool requireActivated = true)
        {
            if (this.Session.UserId == 0)
                throw new WebFaultException(HttpStatusCode.Forbidden);

            if (requireActivated && !this.Db.Users.GetUserById(this.Session.UserId).Activated)
                throw new WebFaultException(HttpStatusCode.Forbidden);
        }
    }
}
