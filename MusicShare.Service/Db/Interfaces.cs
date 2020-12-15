using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Intraction.Db
{
    public interface IMusicShareDbContext
    {
        IUsersRepository Users { get; }

        IDbContext Raw { get; }

        void SubmitChanges();
    }

    public interface IUsersRepository
    {
        void AddUser(DbUserInfo user);
        DbUserInfo GetUserById(long userId);
        DbUserInfo FindUserByLoginKey(string loginKey);
        DbUserInfo FindUserByTokenKey(string key);
    }

}
