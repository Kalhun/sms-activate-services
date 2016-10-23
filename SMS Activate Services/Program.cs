using System;
using System.Collections.Generic;
using SmsServices;

namespace SMS_Activate_Services
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(String.Empty, "sdvr");

            Service service = new Service("keys.txt");

            string num = service.GetNumber();
            string code = service.GetCode();
        }
    }
}