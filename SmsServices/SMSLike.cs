using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmsServices
{
    internal class SmsLike : BaseService
    {
        public SmsLike(string key)
        {
            ApiKey = key;
        }

        private const string ViberKey = "28";

        public float Price
        {
            get
            {
                float price = float.MaxValue;
                try
                {
                    var answer = GetResponse($"http://smslike.ru/index.php?mode=api&apikey={ApiKey}&action=getprices");
                    price = float.Parse(Regex.Match(answer, "(?<=ID28:).*?(?=\\()").Value.Replace(".", ","));
                }
                catch (Exception e)
                {
                    Lgr?.Wl($"{nameof(SmsLike)} {nameof(Price)} {e}");
                }
                return price;
            }
        }
        
        private readonly Dictionary<string, string> _tzidNumber = new Dictionary<string, string>();
        
        public override string GetNumber()
        {
            try
            {
                string tzid;
                string url = "http://smslike.ru/index.php?mode=api" +
                             $"&apikey={ApiKey}&action=regnum&lc=0&s={ViberKey}";
                var answer = GetResponse(url);
                var check = Regex.IsMatch(answer, "OK");
                switch (answer)
                {
                    case "BEFORE_REQUEST_NEW_REPLY_COMPLE_REQUESTED_BEFORE":
                        return null;
                    case "ACTIVE_REQUESTS_LIMIT":
                        return null;
                    case "WARNING_LOW_BALANCE":
                        return null;
                    case "WARNING_NO_NUMS":
                        return null;
                    default:
                        if (check) tzid = answer.Split(':')[1].Trim();
                        else return null;
                        break;
                }

                url = "http://smslike.ru/index.php?mode=api" + $"&apikey={ApiKey}&action=getstate&tz={tzid}";
                answer = GetResponse(url);
                var sw = Stopwatch.StartNew();
                while (!answer.Contains("TZ_NUM_PREPARE") && (sw.ElapsedMilliseconds < 60000))
                {
                    if (answer != "TZ_NUM_WAIT_NUMBER") return null;
                    Thread.Sleep(10500);
                    answer = GetResponse(url);
                }
                sw.Stop();
                var number = Regex.Match(answer, "(?<=TZ_NUM_PREPARE:).*").Value;
                _tzidNumber.Add(number, tzid);
                return number;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsLike)} {nameof(GetNumber)} {e}");
                return null;
            }
        }
        
        public override string GetCodeSms(string number, long timer = 600000)
        {
            try
            {
                string url = "http://smslike.ru/index.php?mode=api" +
                             $"&apikey={ApiKey}&action=setready&tz={_tzidNumber[number]}";
                var answer = GetResponse(url);

                if (!answer.Contains("OK_READY")) return null;

                url = "http://smslike.ru/index.php?mode=api" +
                      $"&apikey={ApiKey}&action=getstate&tz={_tzidNumber[number]}";
                answer = GetResponse(url);

                Stopwatch sw = Stopwatch.StartNew();
                while (!answer.Contains("TZ_NUM_ANSWER") && (sw.ElapsedMilliseconds < timer))
                {
                    if (!answer.Contains("TZ_NUM_WAIT")) return null;
                    Thread.Sleep(10000);
                    answer = GetResponse(url);
                }
                sw.Stop();
                if (answer.Contains("TZ_OVER_EMPTY")) return null;
                return new Regex("(?<=TZ_NUM_ANSWER:).*").Match(answer).Value;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsLike)} {nameof(GetCodeSms)} {e}");
                return null;
            }
        }
    }
}