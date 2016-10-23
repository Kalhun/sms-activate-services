using SmsServices;

namespace SMS_Activate_Services
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Service service = new Service("keys.txt");

            string num = service.GetNumber();
            string code = service.GetCode();
        }
    }
}