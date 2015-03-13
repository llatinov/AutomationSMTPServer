using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace AutomationRhapsody.AutomationSMTPServer
{
    public class SMTPServer : IDisposable
    {
        // 500KB read buffer
        private const int readCount = 512000;
        private const string lineTerminator = "\r\n";
        private const string dataTerminator = "\r\n.\r\n";

        private TcpListener listener;
        private Socket socket;
        private NetworkStream networkStream;
        private StatusEnum status;
        private string path;

        public SMTPServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);

            path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "temp";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private enum StatusEnum
        {
            Connected,
            Identified,
            Mail,
            Recipient,
            Data,
            Disconnected
        }

        private enum ErrorEnum
        {
            SyntaxError = 500,
            ParameterSyntaxError = 501,
            CommandNotImplemented = 502,
            BadCommandSequence = 503,
            CommandParameterNotImplemented = 504,
        }

        public void Start()
        {
            listener.Start();
            status = StatusEnum.Connected;
            Message message = null;
            Console.WriteLine("Start SMTP Server");

            try
            {
                socket = listener.AcceptSocket();
                networkStream = new NetworkStream(socket, FileAccess.ReadWrite, true);
                WriteLine(string.Format("220 Welcome {0}, SMTP Server.", ((IPEndPoint)socket.RemoteEndPoint).Address));
                while (socket.Connected)
                {
                    string data = this.Read(lineTerminator);
                    if (data == null)
                    {
                        break;
                    }
                    else
                    {
                        if (data.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase))
                        {
                            WriteLine("221 Good bye");
                            throw new Exception("Restart SMTP Server");
                        }
                        else if (data.StartsWith("EHLO", StringComparison.InvariantCultureIgnoreCase) || data.StartsWith("HELO", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.WriteGreeting(data.Substring(4).Trim());
                            status = StatusEnum.Identified;
                        }
                        else if (status < StatusEnum.Identified)
                        {
                            this.WriteError(ErrorEnum.BadCommandSequence, "Expected HELO <Your Name>");
                        }
                        else
                        {
                            if (data.StartsWith("MAIL", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (status != StatusEnum.Identified && status != StatusEnum.Data)
                                {
                                    this.WriteError(ErrorEnum.BadCommandSequence, "Command out of sequence");
                                }
                                else
                                {
                                    // create a new message
                                    message = new Message();
                                    message.From = TextFunctions.Tail(data, ":");
                                    status = StatusEnum.Mail;
                                    this.WriteOk();
                                }
                            }
                            else if (data.StartsWith("RCPT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (status != StatusEnum.Mail && status != StatusEnum.Recipient)
                                {
                                    this.WriteError(ErrorEnum.BadCommandSequence, "Command out of sequence");
                                }
                                else
                                {
                                    message.To.Add(TextFunctions.Tail(data, ":"));
                                    status = StatusEnum.Recipient;
                                    this.WriteOk();
                                }
                            }
                            else if (data.StartsWith("DATA", StringComparison.InvariantCultureIgnoreCase))
                            {
                                // request data
                                this.WriteSendData();
                                message.Data = this.Read(dataTerminator);
                                // Remove message end tag
                                message.Data = message.Data.Replace(dataTerminator, string.Empty);
                                SaveMessage(message);
                                message = null;
                                status = StatusEnum.Data;
                                this.WriteOk();
                            }
                            else
                            {
                                this.WriteError(ErrorEnum.CommandNotImplemented, "Command not implemented");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug(e.Message);
                Dispose();
                Start();
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Stop SMTP Server");
            if (socket != null)
            {
                networkStream.Close();
                socket.Close();
                listener.Stop();
                status = StatusEnum.Disconnected;
            }
        }

        #region Read/Write
        private string Read(string terminator)
        {
            var bytes = new byte[readCount];
            var data = new StringBuilder();

            while (true)
            {
                var count = networkStream.Read(bytes, 0, readCount);
                if (count == 0) { break; }
                var dataString = Encoding.UTF8.GetString(bytes, 0, count);
                data.Append(dataString);
                if (dataString.EndsWith(terminator)) { break; }
            }

            Debug(data.ToString());
            return data.ToString();
        }

        private void WriteLine(string data)
        {
            data += lineTerminator;
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            networkStream.Write(bytes, 0, bytes.Length);
            Debug(data);
        }

        private void WriteOk()
        {
            this.WriteLine("250 OK");
        }

        private void WriteError(ErrorEnum error, string description)
        {
            this.WriteLine(String.Format("{0:D} {1}", error, description));
        }

        private void WriteGreeting(string id)
        {
            this.WriteLine(String.Format("250 Hello {0}", id));
        }

        private void WriteSendData()
        {
            this.WriteLine("354 Start mail input; end with <CRLF>.<CRLF>");
        }
        #endregion

        private static void Debug(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new Exception("Zero length data found.");
            }

            data = data.Trim();
            // If multiline message trim only first line
            if (data.IndexOf(Environment.NewLine) > 0)
            {
                data = string.Concat(data.Substring(0, data.IndexOf(Environment.NewLine)), " ...");
            }

            Console.WriteLine("{0:HH:mm:ss.FFF}: {1}", DateTime.Now, data);
        }

        private void SaveMessage(Message e)
        {
            // make sure the subject isn't to long
            string subject = TextFunctions.Remove(e.Subject, Path.GetInvalidFileNameChars());
            if (subject.Length > 100) subject = string.Concat(subject.Substring(0, 100), " ...");
            string fileName = path + Path.DirectorySeparatorChar + e.MessageId + ".eml";
            string mailData = e.XSender + Environment.NewLine + e.XReceiver + Environment.NewLine + e.Data;
            File.WriteAllText(fileName, mailData);
            Console.WriteLine("Message with ID: " + e.MessageId + " saved.");
        }
    }
}