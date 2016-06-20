namespace CappLog
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Reflection;

    public class Log
    {
        private static string loggingTableName = "PDA_Log";
        private static List<Log> instances = new List<Log>();
        private static Log current;
        private IWriter writer;
        private Dictionary<DataColumn, object> staticData;
        private LogType logLevel;
        private LogType emailLevelLoggin;
        private Email emailLoggin;

        #region "AppSql Proxy methods"

        static Log()
        {
            AppSql.SQL.SqLiteAssembly = typeof(SQLiteCommand).Assembly;
        }

        public static Assembly MsSqlAssembly
        {
            get { return AppSql.SQL.MsSqlAssembly; }
            set { AppSql.SQL.MsSqlAssembly = value; }
        }

        public static Assembly SqLiteAssembly
        {
            get { return AppSql.SQL.SqLiteAssembly; }
            set { AppSql.SQL.SqLiteAssembly = value; }
        }

        public static string LoggingTableName
        {
            get { return loggingTableName; }
            set { loggingTableName = value; }
        }

        #endregion

        public static Log Current
        {
            get { return current; }
            set { current = value; }
        }

        public static Log[] RunnedInstances
        {
            get { return instances.ToArray(); }
        }

        public static bool SeparateFileForEachTypeOfRecord = true;

        public static string FileNameDateFormat = "yyyy.MM.dd HH_mm_ss_f";

        public LogType LogLevel
        {
            get { return this.logLevel; }
            set { this.logLevel = value; }
        }

        public LogType EMailLevel
        {
            get
            {
                return this.emailLevelLoggin;
            }

            set
            {
                if (Environment.OSVersion.Platform == PlatformID.WinCE)
                {
                    SystemLogData errorData = new SystemLogData(this.GetType().Name, "set_eMailLevel", new Exception("eMail log is not supported on Windows CE platform!"));
                    this.Write(errorData);
                }
                else if (this.emailLoggin == null)
                {
                    SystemLogData errorData = new SystemLogData(this.GetType().Name, "set_eMailLevel", new Exception("eMail log instance can't be created!"));
                    this.Write(errorData);
                }
                else
                {
                    this.emailLevelLoggin = value;
                }
            }
        }

        public Log(bool sqLiteLog, string param, Dictionary<DataColumn, object> staticData)
        {
            this.staticData = staticData;
            if (sqLiteLog == true)
            {
                this.writer = new SqLiteWriter(this, param);
            }
            else
            {
                this.writer = new FileWriter(param);
            }

            this.logLevel = LogType.UserAction;
            this.emailLevelLoggin = LogType.None;
            if (Environment.OSVersion.Platform != PlatformID.WinCE)
            {
                try
                {
                    this.emailLoggin = new Email(this);
                }
                catch (Exception ex)
                {
                    LogData errorData = new LogData(this.GetType().Name, "New", ex);
                    this.Write(errorData);
                    this.emailLoggin = null;
                }
            }
            else
            {
                this.emailLoggin = null;
            }

            lock (instances)
            {
                instances.Add(this);
            }

            if (current == null)
            {
                current = this;
            }
        }

        public void Constructor(string connectionString, Dictionary<DataColumn, object> staticData)
        {
            if (staticData == null)
            {
                staticData = new Dictionary<DataColumn, object>();
            }

            this.staticData = staticData;
            this.writer = new MsSqlWriter(this, connectionString);

            this.logLevel = LogType.UserAction;
            this.emailLevelLoggin = LogType.None;
            if (Environment.OSVersion.Platform != PlatformID.WinCE)
            {
                try
                {
                    this.emailLoggin = new Email(this);
                }
                catch (Exception ex)
                {
                    SystemLogData errorData = new SystemLogData(this.GetType().Name, "New", ex);
                    this.Write(errorData);
                    this.emailLoggin = null;
                }
            }
            else
            {
                this.emailLoggin = null;
            }

            lock (instances)
            {
                instances.Add(this);
            }

            if (current == null)
            {
                current = this;
            }
        }

        public Log(string connectionString, Dictionary<DataColumn, object> staticData)
        {
            this.Constructor(connectionString, staticData);
        }

        public Log(string connectionString)
        {
            this.Constructor(connectionString, null);
        }

        public static string[] DebugCompiledList()
        {
            int initialized = 0;

#if DEBUG
            initialized = 1;
#endif

            List<string> list = new List<string>();
            if (list.Contains("AppSql.dll") == false)
            {
                list.AddRange(AppSql.SQL.DebugCompiledList());
            }

            if (initialized == 1)
            {
                list.Add("appLog.dll");
            }

            return list.ToArray();
        }

        public LogType StringToEnmLogType(string setting, bool throwIfError)
        {
            switch (setting.Trim().ToUpper())
            {
                case "USERACTION":
                    return LogType.UserAction;
                case "ACTION":
                    return LogType.Action;
                case "WARNING":
                    return LogType.Warning;
                case "ERROR":
                    return LogType.Error;
                case "NONE":
                    return LogType.None;
                default:
                    if (throwIfError == true)
                    {
                        throw new System.Exception("Cant convert '" + setting + "' to enmLogType");
                    }
                    else
                    {
                        SystemLogData vData = new SystemLogData(this.GetType().Name, "StringToEnmLogType", new System.Exception("Cant convert '" + setting + "' to enmLogType"));
                        Write(vData);
                        return LogType.Error;
                    }
            }
        }

        public static LogType StringToEnmLogType(string setting)
        {
            switch (setting.Trim().ToUpper())
            {
                case "USERACTION":
                    return LogType.UserAction;
                case "ACTION":
                    return LogType.Action;
                case "WARNING":
                    return LogType.Warning;
                case "ERROR":
                    return LogType.Error;
                case "NONE":
                    return LogType.None;
                default:
                    return LogType.None;
            }
        }

        public object this[DataColumn field]
        {
            get { return this.staticData[field]; }
            set { this.staticData[field] = value; }
        }

        public IWriter Writer
        {
            get { return this.writer; }
        }

        public Email EMail
        {
            get { return this.emailLoggin; }
        }

        internal DataColumn[] StaticDataColumns
        {
            get
            {
                if (this.staticData == null)
                {
                    return new DataColumn[] { };

                    // Returns an empty array of datacolumn
                }
                else
                {
                    DataColumn[] columns = new DataColumn[this.staticData.Keys.Count];
                    this.staticData.Keys.CopyTo(columns, 0);
                    return columns;
                }
            }
        }

        public void Write(LogData data)
        {
           this.WriteWithoutEmail(data);
            if (!(data.LogTypeCode < Convert.ToInt32(this.emailLevelLoggin)) && this.emailLoggin != null)
            {
                data.StaticData = this.staticData;
                this.emailLoggin.Write(data);
            }
        }

        public void UserAction(string inClass, string inMethod, string message)
        {
            LogData data = new LogData(inClass, inMethod, message, true);
            this.Write(data);
        }

        public void Action(string inClass, string inMethod, string message)
        {
            LogData data = new LogData(inClass, inMethod, message, false);
            this.Write(data);
        }

        public void Warning(string inClass, string inMethod, string message, LogType type)
        {
            LogData data = new LogData(inClass, inMethod, message, LogType.Warning);
            this.Write(data);
        }

        public void Error(string inClass, string inMethod, Exception exception)
        {
            LogData data = new LogData(inClass, inMethod, exception);
            this.Write(data);
        }

        public void Debug(string inClass, string inMethod, string message, LogType type)
        {
            LogData data = new LogData(inClass, inMethod, message, LogType.Debug);
            this.Write(data);
        }

        public void Exception(Exception exception)
        {
            if (exception.InnerException != null)
            {
                this.Exception(exception.InnerException);
            }

            AppLogException customException = AppLogException.Convert(exception);
            string stringClass = customException["Class"];
            string stringMethod = customException["Method"];
            LogData data = new LogData(stringClass, stringMethod, customException);
            this.Write(data);
        }

        /// <summary>
        /// New File name used to rename Log.Db. If file already exist Split will not be performed
        /// </summary>
        /// <param name="archiveFileName"></param>
        /// <remarks></remarks>
        public void Split(string archiveFileName)
        {
            this.writer.Split(archiveFileName);
        }

        public void Close()
        {
            if (this.emailLoggin != null)
            {
                this.emailLoggin.Close();
                this.emailLoggin = null;
            }

            if (this.writer != null)
            {
                this.writer.Close();
                this.writer = null;
            }

            lock (instances)
            {
                instances.Remove(this);
            }

            if (object.ReferenceEquals(current, this))
            {
                current = null;
            }
        }

        internal void WriteWithoutEmail(LogData data)
        {
            if (!(data.LogTypeCode < Convert.ToInt32(this.logLevel)))
            {
                data.StaticData = this.staticData;
                this.writer.Write(data);
            }
        }
    }
}