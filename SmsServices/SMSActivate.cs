using System;
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

        public override string GetNumber()
        {
            try
            {
                string url = "http://sms-activate.ru/stubs/handler_api.php?" +
                          $"api_key={ApiKey}&action=getNumber&service={ViberKey}";
                string answer = GetResponse(url);
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
                string number = Regex.Match(answer, "(?<=\\d:).*").Value;
                if (string.IsNullOrWhiteSpace(number)) throw new Exception($"{nameof(SmsActivate)} {nameof(GetNumber)} Переменная {number} пуста");
                if (TzidNumbers.ContainsKey(number))
                    TzidNumbers[number] = tzid;
                else 
                    TzidNumbers.Add(number, tzid);
                return number;
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
                string url = "http://sms-activate.ru/stubs/handler_api.php?" +
                          $"api_key={ApiKey}&action=setStatus&status=1&id={TzidNumbers[number]}";
                GetResponse(url);

                Stopwatch sw = Stopwatch.StartNew();
                url = "http://sms-activate.ru/stubs/handler_api.php?&" +
                      $"id={TzidNumbers[number]}&api_key={ApiKey}&action=getStatus";
                string answer = GetResponse(url);

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
