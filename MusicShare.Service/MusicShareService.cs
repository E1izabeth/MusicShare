using MusicShare.Interaction;
using MusicShare.Service.Util;
using MusicShare.Service.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using System.Threading.Tasks;
using MusicShare.Interaction.Standard;

namespace MusicShare.Service
{
    public class MusicShareService : IDisposable
    {
        readonly DisposableList _disposables = new DisposableList();

        public MusicShareService(MusicShareServiceConfiguration configuration)
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, ea) => System.Diagnostics.Debug.Print(ea.Exception.ToString());

            var svcCtx = _disposables.Add(new MusicShareServiceContext(configuration));
            var svcHost = _disposables.Add(new ServiceHost(new Impl.MusicShareServiceImpl(svcCtx), new Uri(configuration.ServiceHostUrl)));

            var endPoint = svcHost.AddServiceEndpoint(typeof(IMusicShareSvc), MyWebHttp.CreateBinding(), String.Empty);
            endPoint.Behaviors.Add(MyWebHttp.CreateBehavior(true));
            endPoint.Behaviors.Add(new EnableCorsEndpointBehavior());
            svcHost.Description.Behaviors.Insert(0, new ErrorServiceBehavior());

            svcHost.Description.Behaviors.Add(new AspNetCompatibilityRequirementsAttribute { RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed });

            svcHost.Open();

            Console.WriteLine("Web service started at ");
            foreach (var ep in svcHost.Description.Endpoints)
                Console.WriteLine("\t" + ep.Address.Uri);
        }

        public void Dispose()
        {
            _disposables.SafeDispose();
        }
    }
}
