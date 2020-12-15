using BruTile.Wmts.Generated;
using MusicShare.Interaction;
using MusicShare.Interaction.Standard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicShare.Models
{

    enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete
    }

    enum WebSvcMode
    {
        Xml,
        Json
    }

    class WebApiHelper
    {
        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 60 * 1000;
                return w;
            }
        }

        private Dictionary<HttpMethod, string> _httpMethodNames = new Dictionary<HttpMethod, string>() {
            { HttpMethod.Get, "GET" },
            { HttpMethod.Post, "POST" },
            { HttpMethod.Put, "PUT" },
            { HttpMethod.Delete, "DELETE" }
          };

        private readonly string _rootUrl;
        private readonly WebSvcMode _mode;
        private readonly Dictionary<string, string> _cookies = new Dictionary<string, string>();

        public WebApiHelper(string rootUrl, WebSvcMode mode)
        {
            _rootUrl = rootUrl;
            _mode = mode;
        }

        private string GetCurrentContentType()
        {
            string contentType;
            switch (_mode)
            {
                case WebSvcMode.Xml: contentType = "application/xml"; break;
                case WebSvcMode.Json: contentType = "application/json"; break;
                default:
                    throw new NotImplementedException();
            }
            return contentType;
        }

        private (byte[] obj, string contentType) Serialize(object obj)
        {
            byte[] body;

            if (_mode == WebSvcMode.Xml)
            {
                var xs = new XmlSerializer(obj.GetType());
                var ms = new MemoryStream();
                xs.Serialize(ms, obj);
                ms.Flush();
                body = ms.ToArray();
            }
            else if (_mode == WebSvcMode.Xml)
            {
                var converter = new XmlVsJsonObjectConverter(obj.GetType());
                // var formatter = new JavaScriptSerializer();
                var tree = converter.ToTree(obj, true);
                // body = Encoding.UTF8.GetBytes(formatter.Serialize(tree));
                var bodyText = JsonConvert.SerializeObject(tree);
                body = Encoding.UTF8.GetBytes(bodyText);
            }
            else
            {
                throw new NotImplementedException();
            }

            var contentType = this.GetCurrentContentType();
            return (body, contentType);
        }

        private object Deserialize(Type bodyType, string contentType, byte[] body)
        {
            switch (contentType)
            {
                case "application/xml":
                    {
                        var xs = new XmlSerializer(bodyType);
                        var obj = xs.Deserialize(new MemoryStream(body));
                        return obj;
                    }
                case "application/json":
                    {
                        var converter = new XmlVsJsonObjectConverter(bodyType);
                        //var formatter = new JavaScriptSerializer();
                        //var tree = formatter.DeserializeObject(sr.ReadToEnd());
                        var bodyText = Encoding.UTF8.GetString(body);
                        var tree = JsonConvert.DeserializeObject(bodyText);
                        var obj = converter.FromTree(tree, bodyType);
                        return obj;
                    }
                default:
                    return this.Deserialize(bodyType, this.GetCurrentContentType(), body);
            }
        }

        private async Task<T> PerformRequestAndParse<T>(HttpMethod method, string relativeUrl, object arg = null, bool allowDefault = false)
        {
            var (responseText, contentType) = await this.PerformRequest(method, relativeUrl, arg);
            if (responseText != null && responseText.Length > 0)
            {
                return (T)this.Deserialize(typeof(T), contentType, responseText);
            }
            else if (allowDefault)
            {
                return default(T);
            }
            else
            {
                throw new ApplicationException($"Expected result of type {typeof(T).Name} but has nothing in response from {method} {_rootUrl + relativeUrl}");
            }
        }

        private async Task<(byte[] body, string contentType)> PerformRequest(HttpMethod method, string relativeUrl, object arg = null)
        {
            var url = _rootUrl + relativeUrl;
            var wc = new MyWebClient();
            wc.Encoding = Encoding.UTF8;
            try
            {
                var responseText = await this.PerformRequestImpl(wc, method, url, arg);
                var contentType = wc.ResponseHeaders[HttpRequestHeader.ContentType];
                return (responseText, contentType);
            }
            catch (WebException ex)
            {
                string data, errMsg;
                ErrorInfoType errInfo;
                HttpStatusCode? statusCode;

                if (ex.Response != null)
                {
                    statusCode = (ex.Response as HttpWebResponse)?.StatusCode;

                    if (!string.IsNullOrWhiteSpace(ex.Response.ContentType))
                    {
                        var ms = new MemoryStream();
                        ex.Response.GetResponseStream().CopyTo(ms);

                        errInfo = (ErrorInfoType)this.Deserialize(typeof(ErrorInfoType), ex.Response.ContentType, ms.ToArray());
                        errMsg = errInfo.Message;
                    }
                    else
                    {
                        errInfo = null;
                        if (ex.Response is HttpWebResponse response)
                            errMsg = response.StatusDescription;
                        else
                            errMsg = ex.Message;
                    }
                }
                else
                {
                    statusCode = default(Nullable<HttpStatusCode>);
                    errInfo = null;
                    errMsg = ex.Message;
                }

                System.Diagnostics.Debug.Print($"Response from [{method} {url}] error message: {errMsg}");

                if (errInfo == null)
                    throw new WebApiException(statusCode, errMsg, ex);
                else
                    throw new WebApiException(statusCode, errMsg, errInfo, ex);
            }

        }

        private async Task<byte[]> PerformRequestImpl(WebClient wc, HttpMethod method, string url, object arg)
        {
            wc.Headers[HttpRequestHeader.Accept] = this.GetCurrentContentType();

            if (_cookies.Count > 0)
                wc.Headers[HttpRequestHeader.Cookie] = string.Join(";", _cookies.Select(kv => kv.Key + '=' + kv.Value));

            byte[] result;
            if (arg != null)
            {
                var (body, contentType) = this.Serialize(arg);
                wc.Headers[HttpRequestHeader.ContentType] = contentType;
                result = await wc.UploadDataTaskAsync(url, _httpMethodNames[method], body);
            }
            else
            {
                switch (method)
                {
                    case HttpMethod.Get: result = await wc.DownloadDataTaskAsync(url); break;
                    case HttpMethod.Post: result = await wc.UploadDataTaskAsync(url, "POST", new byte[0]); break;
                    case HttpMethod.Delete: await wc.UploadValuesTaskAsync(url, "DELETE", new NameValueCollection()); result = null; break;
                    case HttpMethod.Put:
                    default:
                        throw new ArgumentException();
                }
            }

            var headers = wc.ResponseHeaders;
            var headerItems = Enumerable.Range(0, headers.Count)
                                  .Select(i => (h: headers.GetKey(i), v: headers.Get(i)))
                                  .ToArray();

            foreach (var (h, v) in headerItems)
            {
                if (h == "Set-Cookie")
                {
                    var kv = v.Split(';')[0].Split('=');
                    _cookies[kv[0]] = kv[1];
                }
            }

            return result;
        }

        public async Task Delete(string url)
        {
            await this.PerformRequest(HttpMethod.Delete, url);
        }

        public async Task Post(string url, object arg = null)
        {
            await this.PerformRequest(HttpMethod.Post, url, arg);
        }

        /*
        public async post<T> (url: string, arg?: Partial<T>): Promise<void> {
          await this.request(HttpMethod.Post, url, arg)
        }
        */

        public async Task<R> PostAndParse<R>(string url, object arg = null)
        {
            return await this.PerformRequestAndParse<R>(HttpMethod.Post, url, arg);
        }

        /*
        public async postAndParse<T, R> (url: string, arg?: Partial<T>): Promise<R> {
          return await this.parse<R>(await this.request(HttpMethod.Post, url, arg))
        }
        */

        public async Task<R> Get<R>(string url)
        {
            return await this.PerformRequestAndParse<R>(HttpMethod.Get, url);
        }

        public async Task<R> GetOrDefault<R>(string url)
        {
            return await this.PerformRequestAndParse<R>(HttpMethod.Get, url, allowDefault: true);
        }
    }


    [Serializable]
    public class WebApiException : ApplicationException
    {
        public HttpStatusCode? StatusCode { get; private set; }
        public ErrorInfoType RemoteErrorInfo { get; private set; }

        public WebApiException() { }
        public WebApiException(string message) : base(message) { }
        public WebApiException(HttpStatusCode? statusCode, string message, Exception inner) : base(message, inner) { this.Setup(statusCode, null); }
        public WebApiException(HttpStatusCode? statusCode, string message, ErrorInfoType errInfo, Exception inner) : base(message, inner) { this.Setup(statusCode, null); }

        protected WebApiException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            info.AddValue("statusCode", this.StatusCode);
            info.AddValue("remoteErrorInfo", this.RemoteErrorInfo);
        }

        private void Setup(HttpStatusCode? statusCode, ErrorInfoType errInfo)
        {
            this.StatusCode = statusCode;
            this.RemoteErrorInfo = errInfo;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            this.StatusCode = (HttpStatusCode?)info.GetValue("statusCode", typeof(HttpStatusCode?));
            this.RemoteErrorInfo = (ErrorInfoType)info.GetValue("remoteErrorInfo", typeof(ErrorInfoType));
            base.GetObjectData(info, context);
        }
    }
}
