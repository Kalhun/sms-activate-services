using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmsServices
{
    internal class SmsReg : BaseService
    {
        public SmsReg(string key)
        {
            ApiKey = key;
        }
        
        private const string ViberKey = "viber";

        public float Price
        {
            get
            {
                float price = float.MaxValue;
                try
                {
                    var answer = GetResponse("https://sms-reg.com/prices.html");
                    price = float.Parse(Regex.Match(answer, "(?<=ps19\">).*?(?=<)").Value.Replace(".", ","));
                }
                catch (Exception e)
                {
                    Lgr?.Wl($"{nameof(SmsReg)} {nameof(Price)} {e}");
                }
                return price;
            }
        }
        
        public override string GetNumber()
        {
            try
            {
                var url = "http://api.sms-reg.com/getNum.php?&country=all" + $"&service={ViberKey}&apikey={ApiKey}";
                var answer = GetResponse(url);

                if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "1") return null;
                var tzid = Regex.Match(answer, "(?<=\"tzid\":\").*?(?=\")").Value;

                url = "http://api.sms-reg.com/getState.php?" + $"tzid={tzid}&apikey={ApiKey}";
                answer = GetResponse(url);
                var sw = Stopwatch.StartNew();
                while (!answer.Contains("TZ_NUM_PREPARE") && (sw.ElapsedMilliseconds < 60000))
                {
                    if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "TZ_INPOOL") return null;
                    Thread.Sleep(10500);
                    answer = GetResponse(url);
                }
                sw.Stop();
                var number = Regex.Match(answer, "(?<=number\":\").*?(?=\")").Value;
                if (string.IsNullOrWhiteSpace(number)) throw new Exception($"Переменная {number} пуста");
                if (TzidNumbers.ContainsKey(number))
                    TzidNumbers[number] = tzid;
                else
                    TzidNumbers.Add(number, tzid);
                return number;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsReg)} {nameof(GetNumber)} {e}");
                return null;
            }
        }
        
        public override string GetCodeSms(string number, long timer = 600000)
        {
            try
            {
                var url = "http://api.sms-reg.com/setReady.php?" + $"tzid={TzidNumbers[number]}&apikey={ApiKey}";
                var answer = GetResponse(url);

                if (new Regex("(?<=response\":\").*?(?=\")").Match(answer).Value != "1") return null;

                url = "http://api.sms-reg.com/getState.php?" + $"tzid={TzidNumbers[number]}&apikey={ApiKey}";
                answer = GetResponse(url);

                var sw = Stopwatch.StartNew();
                while (!answer.Contains("TZ_NUM_ANSWER") && (sw.ElapsedMilliseconds < timer))
                {
                    if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "TZ_NUM_WAIT") return null;
                    Thread.Sleep(10500);
                    answer = GetResponse(url);
                }
                sw.Stop();
                return new Regex("(?<=msg\":\").*?(?=\")").Match(answer).Value;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SmsReg)} {nameof(GetCodeSms)} {e}");
                return null;
            }
        }
    }
}
