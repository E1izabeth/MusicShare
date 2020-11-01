using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Intraction.Db
{
    interface IDbContext : IDisposable
    {
        Table<DbUserInfo> Users { get; }
        Table<DbCarInfo> Cars { get; }
        Table<DbOrderInfo> Orders { get; }

        IDbConnection Connection { get; }

        bool DatabaseExists();
        void CreateDatabase();
        void CreateTables();
    }

    class DbContext : DataContext, IDbContext
    {
        public Table<DbUserInfo> Users { get; }

        IDbConnection IDbContext.Connection { get { return base.Connection; } }

        public DbContext(IDbConnection cnn)
            : base(cnn)
        {
            this.Users = base.GetTable<DbUserInfo>();
        }

        public void CreateTables()
        {
            foreach (var metaTable in this.Mapping.GetTables())
            {
                var cmd = this.Connection.CreateCommand();
                cmd.CommandText = $@"
                                    IF EXISTS (SELECT * FROM sys.tables WHERE name = '{metaTable.TableName}') 
                                        SELECT 1
                                    ELSE
                                        SELECT 0
                                    ";
                var n = (int)cmd.ExecuteScalar();
                if (n < 1)
                {
                    // var metaTable = this.Mapping.GetTable(linqTableClass);
                    var typeName = "System.Data.Linq.SqlClient.SqlBuilder";
                    var type = typeof(DataContext).Assembly.GetType(typeName);
                    var bf = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
                    var sql = type.InvokeMember("GetCreateTableCommand", bf, null, null, new[] { metaTable });
                    var sqlAsString = sql.ToString();
                    this.ExecuteCommand(sqlAsString);
                }
            }
        }
    }

    [Table]
    class DbUserInfo
    {
        [Column(IsPrimaryKey = true, AutoSync = AutoSync.OnInsert, DbType = "BIGINT NOT NULL IDENTITY", IsDbGenerated = true)]
        public long Id { get; set; }
        [Column(DbType = "nvarchar(150)")]
        public string Login { get; set; }
        [Column(DbType = "nvarchar(150)")]
        public string LoginKey { get; set; }
        [Column]
        public DateTime RegistrationStamp { get; set; }
        [Column]
        public DateTime LastLoginStamp { get; set; }

        [Column(DbType = "nvarchar(150)")]
        public string PasswordHash { get; set; }
        [Column(DbType = "nvarchar(150)")]
        public string HashSalt { get; set; }

        [Column(DbType = "nvarchar(150)")]
        public string Email { get; set; }
        [Column]
        public bool Activated { get; set; }

        [Column(DbType = "nvarchar(150)")]
        public string LastToken { get; set; }
        [Column]
        public DateTime LastTokenStamp { get; set; }
        [Column]
        public DbUserTokenKind LastTokenKind { get; set; }

        [Column]
        public bool IsDeleted { get; set; }
    }

    public enum DbUserTokenKind
    {
        Activation,
        AccessRestore
    }
}
