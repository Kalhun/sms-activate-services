using System.IO;
using System.Net;
using System.Text;

namespace SmsServices
{
    internal class BaseService
    {
        protected string ApiKey { get; set; }

        public Logger Lgr { protected get; set; }

        protected static string GetResponse(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 120000;
            return new StreamReader(req.GetResponse().GetResponseStream(), Encoding.GetEncoding(1251)).ReadToEnd();
        }

        public virtual string GetNumber()
        {
            return string.Empty;
        }

        public virtual string GetCodeSms(string number, long timer = 600000)
        {
            return string.Empty;
        }
    }
}
