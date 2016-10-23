using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmsServices
{
    internal class SmsActivate : BaseService
    {
        public SmsActivate(string key)
        {
            ApiKey = key;
        }
        
        public float Price
        {
            get
            {
                float price = float.MaxValue;
                try
                {
                    const string url = "http://sms-activate.ru/";
                    var answer = GetResponse(url);
                    var serv =
                        new Regex("(?<=<tr)[\\w\\W]*?(?=</td></tr>)").Matches(answer)
                            .Cast<Match>()
                            .Single(str => str.Value.Contains("service=\"vi_0\""));
                    price = float.Parse(
                            Regex.Match(Regex.Match(serv.Value, "(?<=\" >).*?(?=</)").Value, "\\d*\\d")
                                .Value.Replace(".", ","));
                }
                catch (Exception e)
                {
                    Lgr?.Wl($"{nameof(SmsActivate)} {nameof(Price)} {e}");
                }
                return price;
            }
        }

        private const string ViberKey = "vi";
        private readonly Dictionary<string, string> _tzidNumber = new Dictionary<string, string>();

        public override string GetNumber()
        {
            try
            {
                var url = "http://sms-activate.ru/stubs/handler_api.php?" +
                          $"api_key={ApiKey}&action=getNumber&service={ViberKey}";
                var answer = GetResponse(url);
                switch (answer)
                {
                    case "NO_NUMBERS":
                        return null;
                    case "NO_BALANCE":
                        return null;
                    case "BAD_ACTION":
                        return null;
                    case "BAD_SERVICE":
                        return null;
                    case "BAD_KEY":
                        return null;
                    case "ERROR_SQL":
                        return null;
                }
                var tzid = Regex.Match(answer, "(?<=ACCESS_NUMBER:).*?(?=:)").Value;
                var num = Regex.Match(answer, "(?<=\\d:).*").Value;
                _tzidNumber.Add(num, tzid);
                return num;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsActivate)} {nameof(GetNumber)} {e}");
                return null;
            }
        }

        public override string GetCodeSms(string number, long timer = 600000)
        {
            try
            {
                var url = "http://sms-activate.ru/stubs/handler_api.php?" +
                          $"api_key={ApiKey}&action=setStatus&status=1&id={_tzidNumber[number]}";
                GetResponse(url);

                var sw = Stopwatch.StartNew();
                url = "http://sms-activate.ru/stubs/handler_api.php?&" +
                      $"id={_tzidNumber[number]}&api_key={ApiKey}&action=getStatus";
                var answer = GetResponse(url);

                while (!answer.Contains("STATUS_OK") && (sw.ElapsedMilliseconds < timer))
                {
                    if (answer != "STATUS_WAIT_CODE") return null;
                    Thread.Sleep(10500);
                    answer = GetResponse(url);
                }
                return new Regex("(?<=STATUS_OK:).*").Match(answer).Value;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsActivate)} {nameof(GetCodeSms)} {e}");
                return null;
            }
        }
    }
}
