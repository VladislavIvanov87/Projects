namespace CappLog
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Threading;

    public class FileWriter : IWriter
    {
        private List<LogData> queue;
        private ManualResetEvent logEvent;
        private bool sleeping;
        private bool started;
        private bool finished;

        private string workingFolder;

        public FileWriter(string path)
        {
            this.workingFolder = path;
            if (Directory.Exists(this.workingFolder) == false)
            {
                Directory.CreateDirectory(this.workingFolder);
            }

            this.queue = new List<LogData>();
            this.logEvent = new ManualResetEvent(false);
            Thread t = new Thread(new ThreadStart(this.Writer));
            t.Name = this.GetType().FullName + ".Writer";
            t.Start();
        }

        public string WorkingFolder
        {
            get { return this.workingFolder; }
        }

        public void Close()
        {
            this.started = false;
            
            // If _Sleeping = True Then
            //    Call _LogEvent.Set()
            // End If
            while (this.finished == false)
            {
                this.logEvent.Set();
                Thread.Sleep(100);
            }
        }

        public void Write(LogData data)
        {
            this.queue.Add(data);
            if (this.sleeping == true)
            {
                this.logEvent.Set();
            }
        }

        public DataTable GetDataToSync(bool allLogs, DateTime beginDate, DateTime endDate)
        {
            return null;
        }

        public int SetDataSync(ref DataTable logTable)
        {
            return -1;
        }

        public int Shrink(int recordsToSave)
        {
            return -1;
        }

        public void Split(string newFileName)
        {
            throw new Exception(this.GetType().FullName + " does not support split functionality!");
        }

        private void Writer()
        {
            this.started = true;
            this.finished = false;
            StringBuilder stringBuilder = new StringBuilder();
            do
            {
                while (this.queue.Count > 0)
                {
                    stringBuilder.Remove(0, stringBuilder.Length);
                    stringBuilder.AppendLine(string.Empty);
                    stringBuilder.Append(string.Format("{0:yyyy-MM-dd HH:mm:ss}", this.queue[0].LogTime) + " ");
                    
                    // "Time", DbType.DateTime
                    stringBuilder.Append(this.queue[0].LogType + " ");
                    
                    // "Category", DbType.String))
                    stringBuilder.Append(this.queue[0].Class + ".");
                    
                    // "Class", DbType.String
                    stringBuilder.AppendLine(this.queue[0].Method);
                    
                    // "Function", DbType.String
                    stringBuilder.AppendLine("\t" + this.queue[0].Description);
                    
                    // "Description", DbType.String
                    // stringBuilder.AppendLine("Sent=0") ' "Sent", DbType.Int32
                    foreach (KeyValuePair<DataColumn, object> keyValuePair in this.queue[0].StaticData)
                    {
                        stringBuilder.AppendLine("\t" + keyValuePair.Key.ColumnName.ToString() + "=" + keyValuePair.Value.ToString());
                    }

                    string fileName = null;

                    if (Log.SeparateFileForEachTypeOfRecord == true)
                    {
                        fileName = string.Format("{0}\\{1}_{2}.Log", this.workingFolder, this.queue[0].LogType, string.Format("{0:" + Log.FileNameDateFormat + "}", this.queue[0].LogTime));
                    }
                    else
                    {
                        fileName = string.Format("{0}\\{1}.Log", this.workingFolder, string.Format("{0:" + Log.FileNameDateFormat + "}", this.queue[0].LogTime));
                    }

                    // End If
                    StreamWriter streamWriter = null;
                    
                    // Loop if someone else has got exclusive access to file
                    do
                    {
                        try
                        {
                            streamWriter = new System.IO.StreamWriter(fileName, true, System.Text.Encoding.UTF8);
                        }
                        catch
                        {
                            streamWriter = null;
                        }
                    }
                    while (streamWriter == null);
                    streamWriter.Write(stringBuilder.ToString());
                    streamWriter.Close();
                    streamWriter.Dispose();
                    streamWriter = null;
                    this.queue.RemoveAt(0);
                }

                if (this.started == true)
                {
                    this.logEvent.Reset();
                    this.sleeping = true;
                    this.logEvent.WaitOne();
                    this.sleeping = false;
                }
            }
            while (this.started == true | this.queue.Count > 0);
            this.finished = true;
        }
    }
}
