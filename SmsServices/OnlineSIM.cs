using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmsServices
{
    internal class OnlineSim : BaseService
    {
        public OnlineSim(string key)
        {
            ApiKey = key;
        }
        
        public float Price => 3;

        private const string ViberKey = "viber";
        
        public override string GetNumber()
        {
            try
            {
                var url = "http://onlinesim.ru/api/getNum.php?" + $"service={ViberKey}&apikey={ApiKey}";
                var answer = GetResponse(url);

                if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "1") return null;
                var tzid = Regex.Match(answer, "(?<=\"tzid\":).*?(?=})").Value;

                url = "http://onlinesim.ru/api/getState.php?" + $"tzid={tzid}&apikey={ApiKey}";
                answer = GetResponse(url);
                var sw = Stopwatch.StartNew();
                while (Regex.Match(answer, "(?<=\"response\":\").*?(?=\")").Value != "1" &&
                       (sw.ElapsedMilliseconds < 60000))
                {
                    if (answer.Contains("TZ_INPOOL"))
                    {
                        Thread.Sleep(10500);
                        answer = GetResponse(url);
                        continue;
                    }
                    if (!answer.Contains("TZ_NUM_WAIT")) return null;
                    var number = new Regex("(?<=number\":\").*?(?=\")").Match(answer).Value;
                    if (string.IsNullOrWhiteSpace(number)) throw new Exception($"{nameof(OnlineSim)} {nameof(GetNumber)} Переменная {number} пуста");
                    if (TzidNumbers.ContainsKey(number))
                        TzidNumbers[number] = tzid;
                    else
                        TzidNumbers.Add(number, tzid);
                    return number;
                }
                sw.Stop();
                return null;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(OnlineSim)} {nameof(GetNumber)} {e}");
                return null;
            }
        }

        public override string GetCodeSms(string number, long timer = 600000)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                string url = "http://onlinesim.ru/api/getState.php?" +
                             $"tzid={TzidNumbers[number]}&message_to_code=1&apikey={ApiKey}";
                string answer = GetResponse(url);
                while (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "TZ_NUM_ANSWER" &&
                       (sw.ElapsedMilliseconds < timer))
                {
                    if (Regex.Match(answer, "(?<=response\":\").*?(?=\")").Value != "TZ_NUM_WAIT") return null;
                    Thread.Sleep(10500);
                    answer = GetResponse(url);
                }
                sw.Stop();
                GetResponse("http://onlinesim.ru/api/setOperationOk.php?" +
                            $"tzid={TzidNumbers[number]}&apikey={ApiKey}");
                return new Regex("(?<=\"msg\":\").*?(?=\")").Match(answer).Value;
            }
            catch (Exception e)
            {
                Lgr?.Wl($"{nameof(OnlineSim)} {nameof(GetCodeSms)} {e}");
                return null;
            }
        }
    }
}
