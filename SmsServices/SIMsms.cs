using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmsServices
{
    internal class SimSms : BaseService
    {
        public SimSms(string apikey)
        {
            ApiKey = apikey;
        }
        
        public float Price
        {
            get
            {
                float price = float.MaxValue;
                try
                {
                    var answer = GetResponse("http://simsms.org/reg-sms.api.php?type=test&country_id=179&operator_id=7");
                    price = float.Parse(Regex.Match(
                        Regex.Matches(answer, "(?<={).*?(?=})").Cast<Match>()
                            .Single(str => str.Value.Contains("service_name\":\"Viber\""))
                            .Value, "(?<=\"service_price\":\").*?(?=\")")
                        .Value.Replace(".", ","));
                }
                catch(Exception e)
                {
                    Lgr?.Wl($"{nameof(SimSms)} {nameof(Price)} {e}");
                }
                return price;
            }
        }

        private const string ViberKey = "opt11";
        private readonly Dictionary<string, string> _tzidNumber = new Dictionary<string, string>();
        
        public override string GetNumber()
        {
            try
            {
                var number =
                    GetResponse("http://simsms.org/priemnik.php?" +
                                $"metod=get_number&country=ru&service={ViberKey}&id=1&apikey={ApiKey}");
                if (Regex.Match(number, "(?<=response\":\").*?(?=\")").Value != "1") return null;

                var tzid = new Regex("(?<=id\":).*?(?=,)").Match(number).Value;
                var num = new Regex("(?<=number\":\").*?(?=\")").Match(number).Value;
                if (string.IsNullOrWhiteSpace(tzid) || string.IsNullOrWhiteSpace(num)) return null;
                _tzidNumber.Add(num, tzid);
                return num;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(SimSms)} {nameof(GetNumber)} {e}");
                return null;
            }
        }

        public override string GetCodeSms(string numer, long timer = 600000)
        {
            try
            {
                var url = "http://simsms.org/priemnik.php?" +
                          $"metod=get_sms&country=ru&service={ViberKey}&id={_tzidNumber[numer]}&apikey={ApiKey}";
                var answer = GetResponse(url);

                var sw = Stopwatch.StartNew();
                while (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "1" &&
                       (sw.ElapsedMilliseconds < timer))
                {
                    if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "2") return null;
                    Thread.Sleep(20000);
                    answer = GetResponse(url);
                }
                sw.Stop();
                return new Regex("(?<=sms\":\").*?(?=\")").Match(answer).Value;
            }catch(Exception e)
            {
                Lgr?.Wl($"{nameof(SimSms)} {nameof(GetCodeSms)} {e}");
                return null;
            }
        }
    }
}
