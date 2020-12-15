using MusicShare.Interaction;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Service
{
    public class MusicShareServiceConfiguration
    {
        // public string DbName { get; set; }
        public string DbFileName { get; set; }
        public string DbConnectionString { get; set; }

        public string ServiceHostUrl { get; set; }

        public TimeSpan SessionTimeout { get; set; }

        public TimeSpan TokenTimeout { get; set; }

        public string SmtpServerHost { get; set; }
        public ushort SmtpServerPort { get; set; }
        public string SmtpLogin { get; set; }
        public string SmtpPassword { get; set; }
        public bool SmtpUseSsl { get; set; }
        public bool SmtpUseDefaultCredentials { get; set; }
        public SmtpDeliveryMethod SmtpDeliveryMethod { get; set; }
        public string SmtpPickupDirectoryLocation { get; set; }

        public MusicShareServiceConfiguration()
        {
        }

        public UriBuilder GetServiceUrl()
        {
            // return new Uri(this.ServiceHostUrl.NormalizeUrl());
            return new UriBuilder(this.ServiceHostUrl.NormalizeUrl());
        }

        public string MakeDbConnectionString()
        {
            var dbFileName = this.DbFileName;
            var dbCnnString = this.DbConnectionString;

            if (!string.IsNullOrWhiteSpace(dbFileName))
            {
                var cnnStringBuilder = new SqlConnectionStringBuilder()
                {
                    IntegratedSecurity = true,
                    // InitialCatalog = dbName,
                    MultipleActiveResultSets = true,
                    DataSource = @"(localdb)\mssqllocaldb",
                    AttachDBFilename = Path.IsPathRooted(dbFileName) ? dbFileName : Path.Combine(Environment.CurrentDirectory, dbFileName),
                    TransparentNetworkIPResolution = false
                };

                var cnnString = cnnStringBuilder.ToString();

                return cnnString;
            }
            else if (!string.IsNullOrWhiteSpace(dbCnnString))
            {
                //var cnnStringBuilder = new SqlConnectionStringBuilder()
                //{
                //    IntegratedSecurity = true,
                //    InitialCatalog = "learningdb1",
                //    MultipleActiveResultSets = true,
                //    DataSource = @"localhost\SQLEXPRESS",
                //    TransparentNetworkIPResolution = false,
                //};

                // localhost\SQLEXPRESS;Database=master;Trusted_Connection=True;
                // var cnnString = cnnStringBuilder.ToString();
                // return cnnString;

                return dbCnnString;
            }
            else
            {
                throw new ApplicationException("Database to use not specified. Use LocalDb file name or exact connection string");
            }
        }
    }
}
