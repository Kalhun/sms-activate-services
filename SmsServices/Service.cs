using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmsServices
{
    public class Service
    {
        public Service(string txtFilePath)
        {
            _currentService = Services.None;
            Logger logger = new Logger();
            var reader = new StreamReader(txtFilePath, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                string[] service = line.Split('|');
                if (string.IsNullOrWhiteSpace(service[1])) continue;

                switch (service[0])
                {
                    case "simsms.org":
                        _simSms = new SimSms(service[1]) {Lgr = logger};
                        break;
                    case "sms-reg.com":
                        _smsReg = new SmsReg(service[1]) {Lgr = logger};
                        break;
                    case "smslike.ru":
                        _smsLike = new SmsLike(service[1]) {Lgr = logger};
                        break;
                    case "sms-activate.ru":
                        _smsActivate = new SmsActivate(service[1]) {Lgr = logger};
                        break;
                    case "onlinesim.ru":
                        _onlineSim = new OnlineSim(service[1]) {Lgr = logger};
                        break;
                }
            }

            _serv = new Dictionary<Services, float?>
            {
                {Services.SmsActivate, _smsActivate?.Price},
                {Services.SimSms, _simSms?.Price},
                {Services.SmsLike, _smsLike?.Price},
                {Services.SmsReg, _smsReg?.Price},
                {Services.OnlineSim, _onlineSim?.Price}
            }.Where(s => s.Value != null).ToDictionary(k => k.Key, v => v.Value);
        }

        private readonly SmsActivate _smsActivate;
        private readonly SmsLike _smsLike;
        private readonly SmsReg _smsReg;
        private readonly SimSms _simSms;
        private readonly OnlineSim _onlineSim;
        private Dictionary<Services, float?> _serv;
        private Services _currentService;
        private string _currentNumber;

        private enum Services
        {
            None,
            SmsActivate,
            SmsLike,
            SimSms,
            OnlineSim,
            SmsReg
        }

        public string GetNumber()
        {
            _serv = new Dictionary<Services, float?>
            {
                {Services.SmsActivate, _smsActivate?.Price},
                {Services.SimSms, _simSms?.Price},
                {Services.SmsLike, _smsLike?.Price},
                {Services.SmsReg, _smsReg?.Price},
                {Services.OnlineSim, _onlineSim?.Price}
            }.Where(s => s.Value != null).ToDictionary(k => k.Key, v => v.Value);
            while (true)
            {
                if (_serv.Count == 0) return null;
                var minPriceServ = _serv.Min(ser => ser.Key);
                switch (minPriceServ)
                {
                    case Services.OnlineSim:
                        var onlineSimNum = _onlineSim?.GetNumber();
                        if (onlineSimNum != null)
                        {
                            _currentNumber = onlineSimNum;
                            _currentService = minPriceServ;
                            return onlineSimNum;
                        }
                        _serv.Remove(minPriceServ);
                        continue;
                    case Services.SimSms:
                        var simSmsNum = _simSms?.GetNumber();
                        if (simSmsNum != null)
                        {
                            _currentNumber = simSmsNum;
                            _currentService = minPriceServ;
                            return simSmsNum;
                        }
                        _serv.Remove(minPriceServ);
                        continue;
                    case Services.SmsActivate:
                        var smsActivateNum = _smsActivate?.GetNumber();
                        if (smsActivateNum != null)
                        {
                            _currentNumber = smsActivateNum;
                            _currentService = minPriceServ;
                            return smsActivateNum;
                        }
                        _serv.Remove(minPriceServ);
                        continue;
                    case Services.SmsLike:
                        var smsLikeNum = _smsLike?.GetNumber();
                        if (smsLikeNum != null)
                        {
                            _currentNumber = smsLikeNum;
                            _currentService = minPriceServ;
                            return smsLikeNum;
                        }
                        _serv.Remove(minPriceServ);
                        continue;
                    case Services.SmsReg:
                        var smsRegNum = _smsReg?.GetNumber();
                        if (smsRegNum != null)
                        {
                            _currentNumber = smsRegNum;
                            _currentService = minPriceServ;
                            return smsRegNum;
                        }
                        _serv.Remove(minPriceServ);
                        continue;
                    default:
                        _currentService = Services.None;
                        return null;
                }
            }
        }

        public string GetCode(long timer = 600000)
        {
            if (_currentNumber == null) return null;
            string code;
            switch (_currentService)
            {
                case Services.None:
                    return null;
                case Services.OnlineSim:
                    code = _onlineSim.GetCodeSms(_currentNumber, timer);
                    _currentNumber = null;
                    _currentService = Services.None;
                    return code;
                case Services.SmsActivate:
                    code = _smsActivate.GetCodeSms(_currentNumber, timer);
                    _currentNumber = null;
                    _currentService = Services.None;
                    return code;
                case Services.SimSms:
                    code = _simSms.GetCodeSms(_currentNumber, timer);
                    _currentNumber = null;
                    _currentService = Services.None;
                    return code;
                case Services.SmsLike:
                    code = _smsLike.GetCodeSms(_currentNumber, timer);
                    _currentNumber = null;
                    _currentService = Services.None;
                    return code;
                case Services.SmsReg:
                    code = _smsReg.GetCodeSms(_currentNumber, timer);
                    _currentNumber = null;
                    _currentService = Services.None;
                    return code;
                default:
                    return null;
            }
        }
    }
}
