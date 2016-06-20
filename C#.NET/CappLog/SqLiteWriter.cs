namespace CappLog
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Text;
    using System.Threading;
    using AppSql;

    internal class SqLiteWriter : IWriter
    {
        private string loggingTableName;
        private bool initialized;
        private Log log;
        private string fullName;
        private SQL sql;
        private FileWriter fileWriter;
        private DbCommand insertCommand;
        private List<LogData> queue;
        private ManualResetEvent logEvent;
        private bool sleeping;
        private Thread thread;
        private bool started;
        private bool isFinished;

        private string workingFolder;

        private ManualResetEvent pauseEvent;

        public SqLiteWriter(Log objLog, string fullName)
        {
            this.log = objLog;
            this.loggingTableName = Log.LoggingTableName;
            this.fullName = fullName;
            this.workingFolder = Path.GetDirectoryName(this.fullName);
            this.pauseEvent = new ManualResetEvent(false);
            this.Init();
        }

        public string WorkingFolder
        {
            get { return this.workingFolder; }
        }

        public void Write(LogData data)
        {
            if (this.initialized == true)
            {
                this.queue.Add(data);
                if (this.sleeping == true)
                {
                    this.logEvent.Set();
                }
            }
            else
            {
                if (data.LogTypeCode == System.Convert.ToInt32(LogType.Error))
                {
                    this.fileWriter.Write(data);
                }
            }
        }

        public int Shrink(int recordsToSave)
        {
            int result = System.Convert.ToInt32(this.sql.Value(string.Format("SELECT COUNT([UID]) AS TotalRecords FROM [{0}];", this.loggingTableName))) - recordsToSave;
            if (result > 0)
            {
                result = this.sql.Exec(string.Format("DELETE FROM [{0}] WHERE [UID] IN (SELECT [UID] FROM [{0}] LIMIT {1});", this.loggingTableName, result.ToString()));
            }

            return result;
        }

        public DataTable GetDataToSync(bool allLogs, DateTime beginDate, DateTime endDate)
        {
            DbParameter[] param =
            {
                this.sql.NewParameter("BeginDate", DbType.Date, beginDate),
                this.sql.NewParameter("EndDate", DbType.Date, endDate)
            };
            string sqlText = null;
            if (allLogs == true)
            {
                sqlText = string.Format("SELECT * FROM [{0}] WHERE ([Sent] = 0 AND [Time] BETWEEN @BeginDate AND @EndDate);", this.loggingTableName);
            }
            else
            {
                sqlText = string.Format("SELECT * FROM [{0}] WHERE ([Sent]=0 AND [Time] BETWEEN @BeginDate AND @EndDate AND Category='Error');", this.loggingTableName);
            }

            DataTable result = this.sql.Select(sqlText, param);
            result.TableName = this.loggingTableName;
            return result;
        }

        public int SetDataSync(ref DataTable logTable)
        {
            int result = 0;
            string sqlText = string.Format("UPDATE [{0}] SET [Sent]=@Sended WHERE [UID]=@UID;", this.loggingTableName);
            DbParameter[] param =
            {
                this.sql.NewParameter("Sended", DbType.Int32, System.Convert.ToInt32(1)),
                this.sql.NewParameter("UID", DbType.String)
            };
            foreach (DataRow row in logTable.Rows)
            {
                param[1].Value = (string)row["UID"];
                result += this.sql.Exec(sqlText, param[0], param[1]);
            }

            return result;
        }

        public void Split(string newFileName)
        {
            newFileName = Path.GetFileName(newFileName);
            newFileName = this.workingFolder + "\\" + newFileName;
            if (File.Exists(newFileName) == false)
            {
                this.Close();
                Thread.Sleep(300);
                File.Move(this.fullName, newFileName);
                this.Init();
            }
        }

        public void Close()
        {
            this.started = false;
            while (this.isFinished == false)
            {
                if (this.sleeping == true)
                {
                    this.started = false;
                    this.logEvent.Set();
                }

                Thread.Sleep(100);
            }

            if (this.insertCommand != null)
            {
                this.insertCommand.Dispose();
                this.insertCommand = null;
            }

            if (this.sql != null)
            {
                this.sql.Close();
                this.sql = null;
            }

            if (this.fileWriter != null)
            {
                this.fileWriter.Close();
                this.fileWriter = null;
            }

            this.thread = null;
            this.initialized = false;
        }

        private void Writer()
        {
            this.started = true;
            this.isFinished = false;
            try
            {
                do
                {
                    while (this.queue.Count > 0)
                    {
                        var with1 = this.insertCommand;
                        with1.Parameters[0].Value = Guid.NewGuid();

                        // ("UID", DbType.String
                        with1.Parameters[1].Value = this.queue[0].LogTime;

                        // "Time", DbType.DateTime
                        with1.Parameters[2].Value = this.queue[0].LogType;

                        // "Category", DbType.String))
                        with1.Parameters[3].Value = this.queue[0].Class;

                        // "Class", DbType.String
                        with1.Parameters[4].Value = this.queue[0].Method;

                        // "Function", DbType.String
                        with1.Parameters[5].Value = this.queue[0].Description;

                        // "Description", DbType.String
                        with1.Parameters[6].Value = System.Convert.ToInt32(0);

                        // "Sent", DbType.Int32
                        // Dim vParameterIdx As Integer = 7
                        foreach (KeyValuePair<DataColumn, object> record in this.queue[0].StaticData)
                        {
                            with1.Parameters[record.Key.ColumnName].Value = record.Value;

                            // vParameterIdx += 1
                        }

                        // RemoveBadCharacters(_InsertCmd)
                        try
                        {
                            with1.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            // _FileWriter.Write(_Queue(0))
                            if (this.queue[0].LogTypeCode == System.Convert.ToInt32(LogType.Error))
                            {
                                this.fileWriter.Write(this.queue[0]);
                            }

                            this.fileWriter.Write(new SystemLogData(this.GetType().FullName, "Writer", ex));
                        }
                        finally
                        {
                            this.queue.RemoveAt(0);
                        }
                    }

                    if (this.started == true)
                    {
                        this.logEvent.Reset();
                        this.sleeping = true;
                        this.logEvent.WaitOne();
                        this.sleeping = false;
                    }
                }
                while (this.started == true || this.queue.Count > 0);
            }
            catch (Exception ex)
            {
                this.fileWriter.Write(new SystemLogData(this.GetType().Name, "Writer", ex));
            }

            this.isFinished = true;
        }

        private void CreateLogTable(SQL sqlClient, string tableName, DataColumn[] dataColumns)
        {
            string stringSQL = string.Empty + "CREATE TABLE IF NOT EXISTS [" + tableName + "]" + " ([UID] CHAR(36) PRIMARY KEY ASC NOT NULL," + " [Time] DATETIME NOT NULL," + " [Category] VARCHAR(20)," + " [Class] VARCHAR(50)," + " [Function] VARCHAR(50)," + " [Description] VARCHAR(4000)," + " [Sent] INTEGER";

            foreach (DataColumn staticColumn in dataColumns)
            {
                stringSQL += ", [" + staticColumn.ColumnName + "] ";
                switch (staticColumn.DataType.Name.ToUpper())
                {
                    case "STRING":
                        stringSQL += "VARCHAR(" + staticColumn.MaxLength.ToString() + ")";
                        break;
                    case "BYTE":
                    case "INTEGER":
                    case "INT16":
                    case "INT32":
                    case "INT64":
                        stringSQL += "INTEGER";
                        break;
                    case "DATE":
                    case "DATETIME":
                        stringSQL += "DATETIME";
                        break;
                    case "SINGLE,DOUBLE,DECIMAL":
                        stringSQL += "DECIMAL(18,2)";
                        break;
                    default:
                        stringSQL += "VARCHAR(4000)";
                        break;
                }
            }

            stringSQL += ");";
            this.sql.Exec(stringSQL);
        }

        private DbCommand GetInsertCommand(SQL sqlClient, DataColumn[] dataColumns)
        {
            string staticFieldNames = this.GetInsertFieldNames(dataColumns);
            if (staticFieldNames == null || staticFieldNames.Length < 1)
            {
                staticFieldNames = " ";
            }

            List<DbParameter> paramList = this.GetInsertParameters(sqlClient, dataColumns);

            DbCommand insertCommand = sqlClient.NewCommand("INSERT INTO " + this.loggingTableName + " (" + "[UID], " + "[Time], " + "[Category], " + "[Class], " + "[Function], " + "[Description], " + "[Sent]" + staticFieldNames.Replace("@", string.Empty) + ") VALUES (" + "@UID, " + "@Time, " + "@Category, " + "@Class, " + "@Function, " + "@Description, " + "@Sent" + staticFieldNames.Replace("[", string.Empty).Replace("]", string.Empty) + ");", paramList);
            return insertCommand;
        }

        private List<DbParameter> GetInsertParameters(SQL sqlClient, DataColumn[] dataColumns)
        {
            List<DbParameter> paramList = new List<DbParameter>();
            paramList.Add(sqlClient.NewParameter("UID", DbType.String));
            paramList.Add(sqlClient.NewParameter("Time", DbType.DateTime));
            paramList.Add(sqlClient.NewParameter("Category", DbType.String));
            paramList.Add(sqlClient.NewParameter("Class", DbType.String));
            paramList.Add(sqlClient.NewParameter("Function", DbType.String));
            paramList.Add(sqlClient.NewParameter("Description", DbType.String));
            paramList.Add(sqlClient.NewParameter("Sent", DbType.Int32));

            if (dataColumns != null)
            {
                foreach (DataColumn staticColumn in dataColumns)
                {
                    switch (staticColumn.DataType.Name.ToUpper())
                    {
                        case "STRING":
                            paramList.Add(sqlClient.NewParameter(staticColumn.ColumnName, DbType.String));
                            break;
                        case "BYTE":
                        case "INTEGER":
                        case "INT16":
                        case "INT32":
                        case "INT64":
                            paramList.Add(sqlClient.NewParameter(staticColumn.ColumnName, DbType.Int64));
                            break;
                        case "DATE":
                        case "DATETIME":
                            paramList.Add(sqlClient.NewParameter(staticColumn.ColumnName, DbType.DateTime));
                            break;
                        case "SINGLE,DOUBLE,DECIMAL":
                            paramList.Add(sqlClient.NewParameter(staticColumn.ColumnName, DbType.Decimal));
                            break;
                        default:
                            paramList.Add(sqlClient.NewParameter(staticColumn.ColumnName, DbType.String));
                            break;
                    }
                }
            }

            return paramList;
        }

        private string GetInsertFieldNames(DataColumn[] dataColumns)
        {
            string staticFiledNames = string.Empty;
            foreach (DataColumn staticColumn in dataColumns)
            {
                staticFiledNames += ", [@" + staticColumn.ColumnName + "] ";
            }

            return staticFiledNames;
        }

        private void RemoveBadCharacters(ref DbCommand command)
        {
            const char FirstChar = ' ';
            if (command != null)
            {
                foreach (DbParameter param in command.Parameters)
                {
                    if (param.DbType == DbType.String && param.Value is string && (param.Value != null))
                    {
                        string value = param.Value.ToString();
                        int index = 0;
                        StringBuilder stringBuilder = new StringBuilder();
                        while (index < value.Length)
                        {
                            if (value[index] < FirstChar)
                            {
                                stringBuilder.Append(FirstChar);
                            }
                            else
                            {
                                stringBuilder.Append(value[index]);
                            }

                            index += 1;
                        }

                        param.Value = stringBuilder.ToString();
                    }
                }
            }
        }

            private void Init()
        {
            this.initialized = false;
            string fileName = Path.GetFileName(this.fullName);
            if (this.fileWriter == null)
            {
                this.fileWriter = new FileWriter(this.workingFolder);
            }

            try
            {
                this.sql = new SQL(SQLClientType.SQLite);
                if (File.Exists(this.fullName) == false)
                {
                    // _Sql.CreateDB(_FullName)
                    this.sql.ConnectionString = "Data Source = " + this.fullName + ";";
                    this.sql.Connection.Open();

                    // CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns)
                }
                else
                {
                    this.sql.ConnectionString = "Data Source = " + this.fullName + ";";

                    // _Sql.Close()
                    // _Sql = Nothing
                    // _Sql = New SQL(SQLClientType.SQLite)
                }

                // Check database for errors and recreate Db if get reoubles with connection
                if (this.sql.DbObjectExist(enmDbObjectType.Table, this.loggingTableName) == false)
                {
                    this.CreateLogTable(this.sql, this.loggingTableName, this.log.StaticDataColumns);
                }

                Action<SqlException> currentErrorHandler = this.sql.ErrorHandler;
                this.sql.ErrorHandler = null;
                try
                {
                    string sqlSelect = string.Format("SELECT COUNT(*) FROM [{0}] LIMIT 1;", this.loggingTableName);
                    this.sql.Value(sqlSelect);
                }
                catch
                {
                    this.sql.Connection.Close();

                    // Build Broken file name
                    string brokenDbFileName = string.Format("{0}\\Broken_{1}_{2}", Path.GetDirectoryName(this.fullName), string.Format("'{0:yyyy-MM-dd}'", DateTime.Now), Path.GetFileName(this.fullName));

                    // Delete existing broken file!
                    // If file does not exist this command 
                    // Will not cause exception so is safe to call without checking!
                    File.Delete(brokenDbFileName);

                    // Rename Log.Db to Broken file name!
                    File.Move(this.fullName, brokenDbFileName);

                    // Creating Log.Db file
                    this.sql.CreateDB(this.fullName);

                    // Another try to connect to Log.Db
                    this.sql.ConnectionString = "Data Source = " + this.fullName + ";";
                    this.CreateLogTable(this.sql, this.loggingTableName, this.log.StaticDataColumns);
                }
                finally
                {
                    this.sql.ErrorHandler = currentErrorHandler;
                }

                if (this.insertCommand == null)
                {
                    this.insertCommand = this.GetInsertCommand(this.sql, this.log.StaticDataColumns);
                }

                if (this.queue == null)
                {
                    this.queue = new List<LogData>();
                }

                if (this.logEvent == null)
                {
                    this.logEvent = new ManualResetEvent(false);
                }

                if (this.thread == null)
                {
                    this.thread = new Thread(new ThreadStart(this.Writer));
                    this.thread.Name = this.GetType().FullName + ".Writer";
                    this.thread.Start();
                }

                this.initialized = true;
            }
            catch (Exception ex)
            {
                this.fileWriter.Write(new SystemLogData(this.GetType().FullName, "New", ex));
                this.initialized = false;
            }
        }
    }
}