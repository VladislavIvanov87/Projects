namespace CappLog
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using AppSql;

    internal class MsSqlWriter : IWriter
    {
        private string loggingTableName;
        private bool initialized;
        private Log log;
        private string connectionString;
        private SQL sql;
        private FileWriter fileWriter;
        private DbCommand insertComand;
        private List<LogData> queue;
        private ManualResetEvent logEvent;
        private Thread thread;

        // Private _Status As Status
        private bool isSleeping;
        private bool isStarted;
        private bool isFinished;

        private string workingFolder;

        private ManualResetEvent pauseEvent;

        public MsSqlWriter(Log objLog, string connectionString)
        {
            if (Environment.OSVersion.Platform == PlatformID.WinCE)
            {
                throw new AppLogException(this.GetType().FullName, "New(appLog.Log, String)", "Unsupported platform!", connectionString);
            }

            this.log = objLog;
            this.loggingTableName = Log.LoggingTableName;
            this.connectionString = connectionString;
            this.workingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (this.workingFolder.ToUpper().StartsWith("FILE:/") == true || this.workingFolder.ToUpper().StartsWith("FILE:\\") == true)
            {
                this.workingFolder = this.workingFolder.Substring(6);
            }

            this.pauseEvent = new ManualResetEvent(false);

            // status = New Status
            this.Init();
        }

        // Private Sub Init()
        // _Initialized = False
        // If _FileWriter Is Nothing Then
        // _FileWriter = New FileWriter(_WorkingFolder)
        // End If
        // Try
        // _Sql = New SQL(SQLClientType.MSSQL)
        // _Sql.ConnectionString = _ConnectionString
        // Dim vParList As New List(Of DbParameter)
        // vParList.Add(_Sql.NewParameter("UID", DbType.String))
        // vParList.Add(_Sql.NewParameter("Time", DbType.DateTime))
        // vParList.Add(_Sql.NewParameter("Category", DbType.String))
        // vParList.Add(_Sql.NewParameter("Class", DbType.String))
        // vParList.Add(_Sql.NewParameter("Function", DbType.String))
        // vParList.Add(_Sql.NewParameter("Description", DbType.String))
        // vParList.Add(_Sql.NewParameter("Sent", DbType.Int32))
        // Dim vStrSQL As String = "" & _
        // "CREATE TABLE IF NOT EXISTS " & _LoggingTableName & _
        // " ([UID] uniqueidentifier PRIMARY KEY NOT NULL," & _
        // " [Time] datetime NOT NULL," & _
        // " [Category] VARCHAR(20)," & _
        // " [Class] VARCHAR(50)," & _
        // " [Function] VARCHAR(50)," & _
        // " [Description] VARCHAR(4000)," & _
        // " [Sent] INT"
        // Dim vStrStaticFiledNames As String = ""
        // For Each vStaticColumn As DataColumn In _Log.StaticDataColumns
        // vStrStaticFiledNames &= ", [@" & vStaticColumn.ColumnName & "] "
        // vStrSQL &= ", [" & vStaticColumn.ColumnName & "] "
        // Select Case vStaticColumn.DataType.Name.ToUpper
        // Case "STRING"
        // vStrSQL &= "VARCHAR(" & vStaticColumn.MaxLength.ToString & ")"
        // vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.String))
        // Case "BYTE", "INTEGER", "INT16", "INT32", "INT64"
        // vStrSQL &= "INT"
        // vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.Int64))
        // Case "DATE", "DATETIME"
        // vStrSQL &= "DATETIME"
        // vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.DateTime))
        // Case "SINGLE,DOUBLE,DECIMAL"
        // vStrSQL &= "DECIMAL(18,2)"
        // vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.Decimal))
        // Case Else
        // vStrSQL &= "VARCHAR(4000)"
        // vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.String))
        // End Select
        // Next
        // vStrSQL &= ");"
        // Call _Sql.Exec(vStrSQL)

        // If _InsertCmd IsNot Nothing Then
        // _InsertCmd.Dispose()
        // _InsertCmd = Nothing
        // End If

        // _InsertCmd = _Sql.NewCommand("INSERT INTO [" & _LoggingTableName & "] (" & _
        // "[UID], " & _
        // "[Time], " & _
        // "[Category], " & _
        // "[Class], " & _
        // "[Function], " & _
        // "[Description], " & _
        // "[Sent]" & _
        // vStrStaticFiledNames.Replace("@", "") & _
        // ") VALUES (" & _
        // "@UID, " & _
        // "@Time, " & _
        // "@Category, " & _
        // "@Class, " & _
        // "@Function, " & _
        // "@Description, " & _
        // "@Sent" & _
        // vStrStaticFiledNames.Replace("[", "").Replace("]", "") & _
        // ");", vParList)
        // If _Queue Is Nothing Then
        // _Queue = New List(Of LogData)
        // End If
        // If _LogEvent Is Nothing Then
        // _LogEvent = New ManualResetEvent(False)
        // End If
        // If _Th Is Nothing Then
        // _Th = New Thread(New ThreadStart(AddressOf Me.Writer))
        // _Th.Start()
        // End If
        // _Initialized = True
        // Catch Ex As Exception
        // _FileWriter.Write(New SysLogData(Me.GetType.FullName, "New", Ex))
        // _Initialized = False
        // End Try
        // End Sub
        public string WorkingFolder
        {
            get { return this.workingFolder; }
        }

        public void Write(LogData data)
        {
            if (this.initialized == true)
            {
                this.queue.Add(data);
                if (this.isSleeping == true)
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
                param[1].Value = row["UID"].ToString();
                result += this.sql.Exec(sqlText, param[0], param[1]);
            }

            return result;
        }

        public void Split(string newFileName)
        {
            throw new AppLogException(this.GetType().FullName, "Split", "This class does not support Split functionality!");
        }

        public void Close()
        {
            this.isStarted = false;
            while (this.isFinished == false)
            {
                if (this.isSleeping == true)
                {
                    this.isStarted = false;
                    this.logEvent.Set();
                }

                Thread.Sleep(100);
            }

            this.insertComand.Dispose();
            this.insertComand = null;
            this.sql.Close();
            this.fileWriter.Close();
            this.fileWriter = null;
            this.thread = null;
            this.initialized = false;
        }

        private void Writer()
        {
            this.isStarted = true;
            this.isFinished = false;
            try
            {
                do
                {
                    while (this.queue.Count > 0)
                    {
                        var with1 = this.insertComand;
                        with1.Parameters[0].Value = System.Guid.NewGuid().ToString();
                        
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

                    if (this.isStarted == true)
                    {
                        this.logEvent.Reset();
                        this.isSleeping = true;
                        this.logEvent.WaitOne();
                        this.isSleeping = false;
                    }
                }
                while (this.isStarted == true || this.queue.Count > 0);
            }
            catch (Exception ex)
            {
                this.fileWriter.Write(new SystemLogData(this.GetType().Name, "Writer", ex));
            }

            this.isFinished = true;
        }

        private void CreateLogTable(SQL sqlClient, string tableName, DataColumn[] dataColumns)
        {
            string stringSQL = string.Empty + "IF  NOT EXISTS (SELECT * FROM [" + tableName + "])" + " CREATE TABLE [" + tableName + "]" + " ([UID] CHAR(36) PRIMARY KEY NOT NULL," + " [Time] DATETIME NOT NULL," + " [Category] VARCHAR(20)," + " [Class] VARCHAR(50)," + " [Function] VARCHAR(50)," + " [Description] VARCHAR(4000)," + " [Sent] INTEGER";

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

            DbCommand insertComad = sqlClient.NewCommand("INSERT INTO " + this.loggingTableName + " (" + "[UID], " + "[Time], " + "[Category], " + "[Class], " + "[Function], " + "[Description], " + "[Sent]" + staticFieldNames.Replace("@", string.Empty) + ") VALUES (" + "@UID, " + "@Time, " + "@Category, " + "@Class, " + "@Function, " + "@Description, " + "@Sent" + staticFieldNames.Replace("[", string.Empty).Replace("]", string.Empty) + ");", paramList);
            return insertComad;
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
            if (this.fileWriter == null)
            {
                this.fileWriter = new FileWriter(this.workingFolder);
            }

            try
            {
                this.sql = new SQL(SQLClientType.MSSQL);
                this.sql.ConnectionString = this.connectionString;

                // Check database for errors and recreate Db if get reoubles with connection
                if (this.sql.DbObjectExist(enmDbObjectType.Table, this.loggingTableName) == false)
                {
                    this.CreateLogTable(this.sql, this.loggingTableName, this.log.StaticDataColumns);
                }

                Action<SqlException> currentErrorHandler = this.sql.ErrorHandler;
                this.sql.ErrorHandler = null;
                try
                {
                    string sqlSelect = string.Format("SELECT COUNT(*) FROM [{0}] ;", this.loggingTableName);
                    this.sql.Value(sqlSelect);
                }
                catch
                {
                    this.sql.Connection.Close();
                    this.sql.ConnectionString = this.connectionString;
                    this.CreateLogTable(this.sql, this.loggingTableName, this.log.StaticDataColumns);
                }
                finally
                {
                    this.sql.ErrorHandler = currentErrorHandler;
                }

                if (this.insertComand != null)
                {
                    this.insertComand.Dispose();
                    this.insertComand = null;
                }

                this.insertComand = this.GetInsertCommand(this.sql, this.log.StaticDataColumns);

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