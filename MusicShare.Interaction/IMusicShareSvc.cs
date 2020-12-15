using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction
{

    [ServiceContract(Namespace = "MusicShareSvc")]
    [XmlSerializerFormat(Style = OperationFormatStyle.Document)]
    public interface IMusicShareSvc
    {
        //[OperationContract, WebInvoke(UriTemplate = "/{*path}", Method = "OPTIONS")]
        //void CorsHandler(string path);

        [OperationContract, WebInvoke(UriTemplate = "/profile?action=register", Method = "POST")]
        void Register(RegisterSpecType registerSpec);
        [OperationContract, WebInvoke(UriTemplate = "/profile?action=restore", Method = "POST")]
        void RequestAccess(ResetPasswordSpecType spec);
        [OperationContract, WebGet(UriTemplate = "/profile?action=activate&key={key}")]
        OkType Activate(string key);
        [OperationContract, WebGet(UriTemplate = "/profile?action=restore&key={key}")]
        OkType RestoreAccess(string key);
        [OperationContract, WebInvoke(UriTemplate = "/profile?action=delete", Method = "POST")]
        void DeleteProfile();

        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/profile?action=login")]
        void Login(LoginSpecType loginSpec);
        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/profile?action=activate")]
        void RequestActivation(RequestActivationSpecType spec);
        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/profile?action=set-email")]
        void SetEmail(ChangeEmailSpecType spec);
        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/profile?action=set-password")]
        void SetPassword(ChangePasswordSpecType spec);
        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/profile?action=logout")]
        void Logout();
        [OperationContract, WebGet(UriTemplate = "/profile")]
        ProfileFootprintInfoType GetProfileFootprint();

        [OperationContract, WebInvoke(UriTemplate = "/error-report")]
        void PushErrorReport(ErrorInfoType errorInfo);
    }
}
