using AutomationRhapsody.AutomationSMTPServer;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;

namespace AutomationRhapsody.AutomationSMTPServerUsage
{
    public class Program
    {
        private static string currentDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
        private static string mailsDir = currentDir + "temp";

        static void Main(string[] args)
        {
            // Clean emails dir
            if (Directory.Exists(mailsDir))
            {
                Directory.Delete(mailsDir, true);
            }

            // Start SMTP Server
            Process smtpServer = new Process();
            smtpServer.StartInfo.FileName = currentDir + "AutomationSMTPServer.exe";
            smtpServer.StartInfo.Arguments = "25";
            smtpServer.Start();

            // Send mails
            SendMail();
            SendMail();
            SendMail("new");

            // Read mails
            string[] files = Directory.GetFiles(mailsDir);
            List<EMLFile> mails = new List<EMLFile>();

            foreach (string file in files)
            {
                EMLFile mail = new EMLFile(file);
                mails.Add(mail);
                File.Delete(file);
            }

            // Compare mails
            bool compare1 = mails[0].Equals(mails[1]);
            bool compare2 = mails[0].Equals(mails[2]);
            bool compare3 = mails[1].Equals(mails[2]);

            // Stop SMTP Server
            smtpServer.Kill();
        }

        private static void SendMail(string suffix = "")
        {
            MailMessage mail = new MailMessage("you@yourcompany.com", "user@yourcompany.com");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "localhost";
            mail.Subject = "this is a test email." + suffix;
            mail.Body = "this is my test email body";
            mail.Attachments.Add(new Attachment(currentDir + "attachment.txt"));
            client.Send(mail);
        }
    }
}
