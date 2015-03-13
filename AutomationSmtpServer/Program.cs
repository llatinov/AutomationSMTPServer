using System;

namespace AutomationRhapsody.AutomationSMTPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string portValue = string.Empty;
            int port = 0;
            if (args != null && args.Length == 1)
            {
                portValue = args[0];
            }
            else
            {
                portValue = System.Configuration.ConfigurationManager.AppSettings["Port"];
            }

            if (!int.TryParse(portValue, out port))
            {
                Console.WriteLine("Port is not found in configuration file. Setting default value to 25.");
                port = 25;
            }
            try
            {
                using (SMTPServer server = new SMTPServer(port))
                {
                    server.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
