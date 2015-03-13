using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace AutomationRhapsody.AutomationSMTPServer
{
    public class Message
    {
        private const string QUOTE_UTF8 = "=?utf-8?q?";
        private const string BASE64_UTF8 = "=?utf-8?b?";

        public Message()
        {
            To = new ArrayList();
        }

        public string XReceiver { get; set; }
        public string XSender { get; set; }
        public string MessageId { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public ArrayList To { get; set; }

        private string data;
        public string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
                string xreceiver = "x-receiver: ";
                foreach (string to in To)
                {
                    string temp = to.Trim().TrimStart('<').TrimEnd('>');
                    if (!data.Contains(temp))
                    {
                        xreceiver += temp + ",";
                    }
                }
                XReceiver = xreceiver.Trim(',');
                XSender = "x-sender: " + From.Trim().TrimStart('<').TrimEnd('>');

                // Get selected header values
                this.MessageId = TextFunctions.Between(data, "message-id:", "\r\n", StringComparison.OrdinalIgnoreCase).Trim().TrimStart('<').TrimEnd('>');
                if (this.MessageId == null || !this.MessageId.Any())
                {
                    this.MessageId = Guid.NewGuid().ToString();
                }
                this.Subject = TextFunctions.Between(data, "subject:", "\r\n", StringComparison.OrdinalIgnoreCase).TrimStart();

                // check for special encoding
                if (this.Subject.StartsWith(BASE64_UTF8, StringComparison.InvariantCultureIgnoreCase))
                {
                    // subject has been encoded base 64
                    this.Subject = Encoding.UTF8.GetString(Convert.FromBase64String(this.Subject.Substring(BASE64_UTF8.Length, this.Subject.LastIndexOf("?=") - BASE64_UTF8.Length)));
                }
            }
        }
    }
}
