using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AutomationRhapsody.AutomationSMTPServer
{
    public class EMLFile
    {
        private const string BASE64_UTF8 = "=?utf-8?b?";
        private const string CONTENT_TYPE = "content-type";
        public string X_Sender { get; set; }
        public string[] X_Receivers { get; set; }
        public string Received { get; set; }
        public string Mime_Version { get; set; }
        public string From { get; set; }
        public string[] To { get; set; }
        public string CC { get; set; }
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public string Content_Type { get; set; }
        public string Content_Transfer_Encoding { get; set; }
        public string Return_Path { get; set; }
        public string Message_ID { get; set; }
        public DateTime X_OriginalArrivalTime { get; set; }
        public string Body { get; set; }
        public byte[] Image { get; set; }
        public List<byte[]> Attachments { get; set; }
        public Dictionary<string, string> UnsupportedHeaders { get; set; }

        public EMLFile(string path)
        {
            ParseEML(new FileStream(path, FileMode.Open));
        }

        public EMLFile(FileStream fsEML)
        {
            ParseEML(fsEML);
        }

        private void ParseEML(FileStream fsEML)
        {
            Attachments = new List<byte[]>();
            StreamReader sr = new StreamReader(fsEML);
            string sLine;
            // Read file to a list
            List<string> listAll = new List<string>();
            while ((sLine = sr.ReadLine()) != null)
            {
                listAll.Add(sLine);
            }
            fsEML.Close();
            int bodyStart = -1;
            // Convert to array
            string[] allContent = new string[listAll.Count];
            listAll.CopyTo(allContent);
            // Itterate array, merge continuation lines and add result to new list
            listAll = new List<string>();
            List<string> header = new List<string>();
            for (int i = 0; i < allContent.Length; i++)
            {
                string sFullValue = allContent[i];
                MergeSplitValue(allContent, ref i, ref sFullValue);
                listAll.Add(sFullValue);
                // If body has not started yet add value to header list
                if (bodyStart == -1)
                {
                    header.Add(sFullValue);
                }
                // Body starts with first empty line. If empty line is found and body has not started yet then this is body start line number
                if (allContent[i] == string.Empty && bodyStart == -1)
                {
                    bodyStart = i;
                }
            }

            // Transfer list to array to work with
            allContent = new string[listAll.Count];
            listAll.CopyTo(allContent);

            // Set main EMLFile field from header
            SetFields(header.ToArray());

            // Exit in case of no body
            if (bodyStart == -1)
            {
                return;
            }

            // Multipart email with HTML body
            if (Content_Type != null && (Content_Type.ToLower().Contains("multipart")))
            {
                ParseMultipart(allContent);
            }
            // Plain text body type only
            else
            {
                Body = string.Empty;
                for (int n = bodyStart + 1; n < allContent.Length; n++)
                {
                    Body += allContent[n] + "\r\n";
                }
            }

            if ("base64".Equals(Content_Transfer_Encoding))
            {
                Body = Encoding.UTF8.GetString(Convert.FromBase64String(Body));
            }

            Body = FormatBody(Body);
        }

        private void ParseMultipart(string[] content)
        {
            string boundaryString = "boundary=";
            bool isSingleContentPart = true;
            string boundaryMarker = string.Empty;
            List<string> subContent = new List<string>();
            foreach (string line in content)
            {
                // If this is content type line and there is boundary started and there is no boundary marker extracted
                if (line.ToLower().StartsWith(CONTENT_TYPE) && line.ToLower().Contains(boundaryString) && boundaryMarker == string.Empty)
                {
                    // Mark multicontent part of message
                    isSingleContentPart = false;
                    // Get boundary marker
                    int ix = line.ToLower().IndexOf(boundaryString);
                    boundaryMarker = line.Substring(ix + boundaryString.Length).Trim(new char[] { '=', '"', ' ', '\t' });
                }
                // If boundary marker is defined and line doesn't contain boundary marker then add it to sub content list
                else if (boundaryMarker != string.Empty && !line.Contains(boundaryMarker))
                {
                    subContent.Add(line);
                }
                // If line contans boundary marker and there is some subcontent added then recursively itterate subcontent
                else if (line.Contains(boundaryMarker) && subContent.Any())
                {
                    ParseMultipart(subContent.ToArray());
                    subContent = new List<string>();
                }
            }

            if (isSingleContentPart)
            {
                SetContent(content);
            }
        }

        private void SetContent(string[] content)
        {
            bool isBody = false;
            bool isAttachment = false;
            bool isImage = false;
            bool contentStart = false;
            List<string> contentList = new List<string>();

            foreach (string line in content)
            {
                // Check type of content
                if (line.ToLower().StartsWith(CONTENT_TYPE))
                {
                    // If text
                    if (line.ToLower().Contains("text/plain") || line.ToLower().Contains("text/html"))
                    {
                        isBody = true;
                    }
                    // If attachment. Correct PDF attachemnt has "Content-Type: application/pdf" and its name is in Content-Disposition. Incorrect PDF attachment has name inside "Content-Type: application/pdf; name="FileName.pdf""
                    else if (line.ToLower().Contains("application/pdf") && !line.Contains(";"))
                    {
                        isAttachment = true;
                    }
                    // If image
                    else if (line.ToLower().Contains("image/jpeg"))
                    {
                        isImage = true;
                    }
                }
                // Set content encoding if not already set
                else if (line.ToLower().StartsWith("content-transfer-encoding") && string.IsNullOrWhiteSpace(Content_Transfer_Encoding))
                {
                    SetFields(new string[] { line });
                }
                // If line is empty and content hasn't strated then start content
                else if (line == string.Empty && !contentStart)
                {
                    contentStart = true;
                }
                // If content has started then add line to content list
                else if (contentStart)
                {
                    contentList.Add(line);
                }
            }

            if (isBody)
            {
                Body = string.Join("\r\n", contentList.ToArray());
            }
            else if (isAttachment)
            {
                Attachments.Add(Convert.FromBase64String(string.Join("\r\n", contentList.ToArray())));
            }
            else if (isImage)
            {
                Image = Convert.FromBase64String(string.Join("\r\n", contentList.ToArray()));
            }
        }

        private void MergeSplitValue(string[] sa, ref int i, ref string sValue)
        {
            if (i + 1 < sa.Length && sa[i + 1] != string.Empty && char.IsWhiteSpace(sa[i + 1], 0))   // spec says line's that begin with white space are continuation lines
            {
                i++;
                sValue += " " + sa[i].Trim();
                MergeSplitValue(sa, ref i, ref sValue);
            }
        }

        private void SetFields(string[] saLines)
        {
            UnsupportedHeaders = new Dictionary<string, string>();
            List<string> listX_Receiver = new List<string>();
            foreach (string sHdr in saLines)
            {
                string[] saHdr = Split(sHdr);
                // not a valid header
                if (saHdr == null)
                {
                    continue;
                }

                switch (saHdr[0].ToLower())
                {
                    case "x-sender":
                        X_Sender = saHdr[1];
                        break;
                    case "x-receiver":
                        listX_Receiver.Add(saHdr[1]);
                        break;
                    case "received":
                        Received = saHdr[1];
                        break;
                    case "mime-version":
                        Mime_Version = saHdr[1];
                        break;
                    case "from":
                        From = saHdr[1];
                        break;
                    case "to":
                        To = saHdr[1].Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "cc":
                        CC = saHdr[1];
                        break;
                    case "date":
                        Date = DateTime.Parse(saHdr[1]);
                        break;
                    case "subject":
                        Subject = saHdr[1];
                        if (Subject.ToLower().Contains(BASE64_UTF8))
                        {
                            string[] parts = Subject.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            string temp = string.Empty;
                            foreach (string part in parts)
                            {
                                temp += part.Substring(BASE64_UTF8.Length, part.LastIndexOf("?=") - BASE64_UTF8.Length);
                            }
                            Subject = Encoding.UTF8.GetString(Convert.FromBase64String(temp));
                        }
                        break;
                    case CONTENT_TYPE:
                        Content_Type = saHdr[1];
                        break;
                    case "content-transfer-encoding":
                        Content_Transfer_Encoding = saHdr[1];
                        break;
                    case "return-path":
                        Return_Path = saHdr[1];
                        break;
                    case "message-id":
                        Message_ID = saHdr[1];
                        break;
                    case "x-originalarrivaltime":
                        int ix = saHdr[1].IndexOf("FILETIME");
                        if (ix != -1)
                        {
                            string sOAT = saHdr[1].Substring(0, ix);
                            sOAT = sOAT.Replace("(UTC)", "-0000");
                            X_OriginalArrivalTime = DateTime.Parse(sOAT);
                        }
                        break;
                    default:
                        UnsupportedHeaders.Add(saHdr[0], saHdr[1]);
                        break;
                }
            }

            X_Receivers = new string[listX_Receiver.Count];
            listX_Receiver.CopyTo(X_Receivers);
        }

        private string[] Split(string sHeader)  // because string.Split won't work here...
        {
            int ix;
            if ((ix = sHeader.IndexOf(':')) == -1)
            {
                return null;
            }
            return new string[] { sHeader.Substring(0, ix).Trim(), sHeader.Substring(ix + 1).Trim() };
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EMLFile))
            {
                return false;
            }

            EMLFile compare = (EMLFile)obj;

            if (!CompareCollections(this.X_Receivers, compare.X_Receivers) || !this.X_Sender.Equals(compare.X_Sender) ||
                !CompareCollections(this.To, compare.To) || !this.From.Equals(compare.From) ||
                !this.Subject.Equals(compare.Subject) || !this.Body.Equals(compare.Body) ||
                !CompareCollections(this.Image, compare.Image) || !CompareCollections(this.Attachments, compare.Attachments))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append("Subject: " + this.Subject);
            result.Append(", From: " + this.From);
            result.Append(", To: " + string.Join(", ", this.To));
            result.Append(", Receivers: " + String.Join(", ", this.X_Receivers));
            result.Append(", Senders: " + this.X_Sender);
            result.Append(", Body: " + this.Body);
            return result.ToString();
        }

        private bool CompareCollections<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null && second == null)
            {
                return true;
            }
            else if (first != null && second != null)
            {
                return Enumerable.SequenceEqual(first, second);
            }
            else
            {
                return false;
            }
        }

        private string FormatBody(string value)
        {
            // If Body is HTML
            if (value.Contains("<html>"))
            {
                try
                {
                    // Replace ampersand only if it is not part of some escape sequence
                    XElement xElement = XElement.Parse(Regex.Replace(value, "&(?!(amp|apos|quot|lt|gt|#\\d{2,4});)", "&amp;"));
                    return xElement.ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error found during HTML parse: " + e.Message + ". Content: " + value);
                }
            }
            return value;
        }
    }
}
