namespace CappLog
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;

    public class Email
    {
        private string sender;
        private string receiver;
        private string[] arrTo;
        private string subject;
        private List<LogData> queue;
        private string host;
        private int port;
        private ManualResetEvent logEvent;
        private bool started;
        private bool finished;
        private Log log;

        private Action<MailData> emailSender;

        public Email(Log log)
        {
            this.log = log;
            this.queue = new List<LogData>();
            this.logEvent = new ManualResetEvent(false);
            this.subject = "$EVENTTYPE$>$CLASS$>$METHOD$";
            Thread t = new Thread(new ThreadStart(this.Send));
            t.Name = this.GetType().FullName + ".Send";
            t.Start();
        }

        public Action<MailData> EMailSender
        {
            get { return this.emailSender; }
            set { this.emailSender = value; }
        }

        public string Sender
        {
            get
            {
                return this.sender;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.sender = value;
            }
        }

        public string Receiver
        {
            get
            {
                string result = string.Empty;

                if (this.arrTo != null)
                {
                    foreach (string received in this.arrTo)
                    {
                        result += "; " + received;
                    }

                    if (result.Length > 0)
                    {
                        result = result.Substring(2);
                    }
                }

                return result;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.receiver = value;

                List<string> arrTo = new List<string>();

                foreach (string received in this.receiver.Split(',', ';'))
                {
                    if (received.Trim().Length > 0)
                    {
                        arrTo.Add(received);
                    }
                }

                this.arrTo = arrTo.ToArray();
            }
        }

        public string Subject
        {
            get
            {
                return this.subject;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.subject = value;
            }
        }

        public string Host
        {
            get
            {
                return this.host;
            }

            set
            {
                try
                {
                    if (value == null)
                    {
                        value = string.Empty;
                    }

                    int delimeterIndex = value.LastIndexOf(Convert.ToChar(":"));
                    if (delimeterIndex > 0)
                    {
                        this.Port = int.Parse(value.Substring(1 + delimeterIndex));
                        this.host = value.Substring(0, delimeterIndex);
                    }
                    else
                    {
                        this.host = value;
                    }
                }
                catch (Exception ex)
                {
                    this.log.WriteWithoutEmail(new LogData(this.GetType().Name, "set_Host", new Exception("Value=" + value + ". " + ex.Message)));
                }
            }
        }

        public int Port
        {
            get { return this.port; }
            set { this.port = value; }
        }

        public void Write(LogData data)
        {
            this.queue.Add(data);
            this.logEvent.Set();
        }

        public void Close()
        {
            this.started = false;
            while (false == this.finished)
            {
                this.logEvent.Set();
                Thread.Sleep(200);
            }
        }

        private void Send()
        {
            this.started = true;
            this.finished = false;
            try
            {
                do
                {
                    while (this.queue.Count > 0)
                    {
                        try
                        {
                            if (this.emailSender != null)
                            {
                                System.Text.StringBuilder stringBuilder = new StringBuilder();
                                stringBuilder.AppendLine("===");
                                stringBuilder.AppendLine("UID=" + Guid.NewGuid().ToString());
                                
                                // ("UID", DbType.String
                                stringBuilder.AppendLine("Time=" + string.Format("'{0:yyyy-MM-dd HH:mm:ss}'", this.queue[0].LogTime));
                                
                                // "Time", DbType.DateTime
                                stringBuilder.AppendLine("LogType=" + this.queue[0].LogType);
                                
                                // "Category", DbType.String
                                stringBuilder.AppendLine("Class=" + this.queue[0].Class);
                                
                                // "Class", DbType.String
                                stringBuilder.AppendLine("Method=" + this.queue[0].Method);
                                
                                // "Function", DbType.String
                                stringBuilder.AppendLine("Description=" + this.queue[0].Description);
                                
                                // "Description", DbType.String
                                stringBuilder.AppendLine("Sent=0");
                                
                                // "Sent", DbType.Int32
                                foreach (KeyValuePair<DataColumn, object> keyValuePair in this.queue[0].StaticData)
                                {
                                    stringBuilder.AppendLine(keyValuePair.Key.ColumnName + "=" + keyValuePair.Value.ToString());
                                }

                                MailData messageData = new MailData();
                                var with1 = messageData;
                                with1.Sender = this.sender;
                                with1.Receiver = this.arrTo;
                                with1.Host = this.host;
                                with1.Port = this.port;
                                with1.Subject = this.subject.Replace("$EVENTTYPE$", this.queue[0].LogType).Replace("$CLASS$", this.queue[0].Class).Replace("$METHOD$", this.queue[0].Method);
                                with1.Body = stringBuilder.ToString();
                                stringBuilder = null;
                                this.emailSender(messageData);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogData errorData = new LogData(this.GetType().Name, "Send", ex);
                            this.log.WriteWithoutEmail(errorData);
                        }

                        this.queue.RemoveAt(0);
                        Thread.Sleep(200);
                    }

                    this.logEvent.Reset();
                    this.logEvent.WaitOne();
                }
                while (this.started == true);
            }
            catch (Exception ex)
            {
                LogData errorData = new LogData(this.GetType().Name, "Send", ex);
                this.log.WriteWithoutEmail(errorData);
                this.started = false;
            }

            this.finished = true;
        }
    }

    public struct MailData
    {
        private string sender;
        private string[] receiver;
        private string host;
        private int port;
        private string subject;
        private string body;

        public string Sender
        {
            get
            {
                return this.sender;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.sender = value;
            }
        }

        public string[] Receiver
        {
            get { return this.receiver; }
            set { this.receiver = value; }
        }

        public string Host
        {
            get
            {
                return this.host;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.host = value;
            }
        }

        public int Port
        {
            get { return this.port; }
            set { this.port = value; }
        }

        public string Subject
        {
            get
            {
                return this.subject;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.subject = value;
            }
        }

        public string Body
        {
            get
            {
                return this.body;
            }

            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.body = value;
            }
        }
    }
}