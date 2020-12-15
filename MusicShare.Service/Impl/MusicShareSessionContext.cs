using MusicShare.Intraction.Db;
using MusicShare.Service.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Service.Impl
{
    class MusicShareSessionContext : IMusicShareSessionContext
    {
        public LinkedListNode<MusicShareSessionContext> ListNode { get; private set; }

        public Guid Id { get; private set; }
        public DateTime LastActivity { get; private set; }
        public long UserId { get; private set; }
        public bool IsActivated { get; private set; }

        public event Action<long> OnUserContextChanging = delegate { };

        public MusicShareSessionContext()
        {
            this.Id = Guid.NewGuid();
            this.Renew();

            this.ListNode = new LinkedListNode<MusicShareSessionContext>(this);
        }

        public void Renew()
        {
            this.LastActivity = DateTime.UtcNow;
        }

        public void SetUserContext(DbUserInfo user)
        {
            var userId = user?.Id ?? 0;
            this.OnUserContextChanging(userId);
            this.UserId = userId;

            this.IsActivated = user?.Activated ?? false;
        }
    }
}
