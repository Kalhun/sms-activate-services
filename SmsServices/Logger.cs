using System;
using System.IO;

namespace SmsServices
{
    internal class Logger
    {
        public Logger()
        {
            _writer = new StreamWriter(Directory.GetCurrentDirectory() + "/SmsServices.dll log.txt", true);
        }
        
        private readonly StreamWriter _writer; 
        
        public async void Wl(string log)
        {
            await _writer.WriteLineAsync($"[{DateTime.Now}]  {log}");
        }
    }
}