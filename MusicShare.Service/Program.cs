using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Xml;
using MusicShare.Service.Impl;
using MusicShare.Interaction;
using MusicShare.Service.Util;

namespace MusicShare.Service
{
    class Program
    {
        //public IEnumerable<XmlDocumentDto> GetXmlDocumentsRange(int from, int count)
        //{
        //    var Db = new DbContext();
        //    return Db.Docs
        //             .Where(x => x.CreatedOn >= DateTime.Today && x.CreatedOn < DateTime.Today.AddDays(1))
        //             .Skip(from).Take(count)
        //             .Select(x => new XmlDocumentDto { ...})
        //             .ToList();
        //}

        // --= LocalDb Controls =--
        // list the instancies
        //   sqllocaldb i
        // stop selected instance
        //   sqllocaldb p "selected instance"
        // delete
        //   sqllocaldb d "selected instance"
        // recreate or create new one 
        //   sqllocaldb c "new instance"

        static void Test()
        {
            throw new Exception("asdf");
        }

        static void Main(string[] args)
        {
            var cfg = new MusicShareServiceConfiguration()
            {
                // DbName = "appdata",
                // DbFileName = "mydbfilename4.mdf",
                DbConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=MusicSharedb;Integrated Security=True;MultipleActiveResultSets=True;TransparentNetworkIPResolution=False",
                //DbConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=learningapp-testdb1;Integrated Security=True;MultipleActiveResultSets=True;TransparentNetworkIPResolution=False",
                ServiceHostUrl = "http://0.0.0.0:8181/mysvc/",
                SessionTimeout = TimeSpan.FromHours(1),
                TokenTimeout = TimeSpan.FromDays(7),

                SmtpServerHost = "smtp.live.com",
                SmtpServerPort = 587,
                SmtpLogin = "ipm.local@hotmail.com",
                SmtpPassword = "ifmo2020",
                SmtpUseSsl = true,
                SmtpUseDefaultCredentials = false,
                SmtpDeliveryMethod = SmtpDeliveryMethod.Network
                //SmtpLogin = "MusicShare@localhost",
                //SmtpServerHost = "127.0.0.1",
                //SmtpServerPort = 25,
                //SmtpUseSsl = false,
                //SmtpUseDefaultCredentials = true,
                //SmtpDeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                //SmtpPickupDirectoryLocation = @"D:\Temp\"
            };

            using (var svc = new MusicShareService(cfg))
            {
                Console.WriteLine();
                Console.WriteLine("Press Q to stop");
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
        }
    }
}
