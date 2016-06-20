
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Data.Common;
using System.Threading;
using AppSql;
using System.Text;

internal class SqLiteWriter : IWriter
{


    private string _LoggingTableName;
    private bool _Initialized;
    private Log _Log;
    private string _FullName;
    private SQL _Sql;
    private FileWriter _FileWriter;
    private DbCommand _InsertCmd;
    private List<LogData> _Queue;
    private ManualResetEvent _LogEvent;
    private bool _Sleeping;
    private System.Threading.Thread _Th;
    private bool _Started;
    private bool _IsFinished;

    private string _WorkingFolder;

    private ManualResetEvent _PauseEvent;

    public SqLiteWriter(Log ObjLog, string FullName)
    {
        _Log = ObjLog;
        _LoggingTableName = Log.LoggingTableName;
        _FullName = FullName;
        _WorkingFolder = System.IO.Path.GetDirectoryName(_FullName);
        _PauseEvent = new ManualResetEvent(false);
        Init();
    }

    private void Init()
    {
        _Initialized = false;
        string vFileName = System.IO.Path.GetFileName(_FullName);
        if (_FileWriter == null)
        {
            _FileWriter = new FileWriter(_WorkingFolder);
        }
        try
        {
            _Sql = new SQL(SQLClientType.SQLite);
            if (System.IO.File.Exists(_FullName) == false)
            {
                //_Sql.CreateDB(_FullName)
                _Sql.ConnectionString = "Data Source = " + _FullName + ";";
                _Sql.Connection.Open();
                //CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns)
            }
            else
            {
                _Sql.ConnectionString = "Data Source = " + _FullName + ";";
                //_Sql.Close()
                //_Sql = Nothing
                //_Sql = New SQL(SQLClientType.SQLite)
            }

            //Check database for errors and recreate Db if get reoubles with connection
            if (_Sql.DbObjectExist(enmDbObjectType.Table, _LoggingTableName) == false)
            {
                CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns);
            }
            Action<SqlException> vCurrentErrorHandler = _Sql.ErrorHandler;
            _Sql.ErrorHandler = null;
            try
            {
                string vStrSqlSelect = string.Format("SELECT COUNT(*) FROM [{0}] LIMIT 1;", _LoggingTableName);
                _Sql.Value(vStrSqlSelect);
            }
            catch 
            {
                _Sql.Connection.Close();
                //Build Broken file name
                string vBrokenDbFileName = string.Format("{0}\\Broken_{1}_{2}", System.IO.Path.GetDirectoryName(_FullName), String.Format("'{0:yyyy-MM-dd}'", System.DateTime.Now), System.IO.Path.GetFileName(_FullName));
                //  Delete existing broken file!
                //  If file does not exist this command 
                //will not cause exception so is safe to call without checking!
                System.IO.File.Delete(vBrokenDbFileName);
                //  Rename Log.Db to Broken file name!
                System.IO.File.Move(_FullName, vBrokenDbFileName);
                //  Creating Log.Db file
                _Sql.CreateDB(_FullName);
                //Another try to connect to Log.Db
                _Sql.ConnectionString = "Data Source = " + _FullName + ";";
                CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns);
            }
            finally
            {
                _Sql.ErrorHandler = vCurrentErrorHandler;
            }


            if (_InsertCmd == null)
            {
                _InsertCmd = GetInsertCommand(_Sql, _Log.StaticDataColumns);
            }

            if (_Queue == null)
            {
                _Queue = new List<LogData>();
            }
            if (_LogEvent == null)
            {
                _LogEvent = new ManualResetEvent(false);
            }
            if (_Th == null)
            {
                _Th = new Thread(new ThreadStart(this.Writer));
                _Th.Name = this.GetType().FullName + ".Writer";
                _Th.Start();
            }
            _Initialized = true;
        }
        catch (Exception Ex)
        {
            _FileWriter.Write(new SysLogData(this.GetType().FullName, "New", Ex));
            _Initialized = false;
        }
    }

    public string WorkingFolder
    {
        get { return _WorkingFolder; }
    }

    public void Write(LogData Data)
    {
        if (_Initialized == true)
        {
            _Queue.Add(Data);
            if (_Sleeping == true)
            {
                _LogEvent.Set();
            }
        }
        else
        {
            if (Data.LogTypeCode == System.Convert.ToInt32(enmLogType.Error))
            {
                _FileWriter.Write(Data);
            }
        }
    }

    public int Shrink(int RecordsToSave)
    {
        int Result = System.Convert.ToInt32(_Sql.Value(string.Format("SELECT COUNT([UID]) AS TotalRecords FROM [{0}];", _LoggingTableName))) - RecordsToSave;
        if (Result > 0)
        {
            Result = _Sql.Exec(string.Format("DELETE FROM [{0}] WHERE [UID] IN (SELECT [UID] FROM [{0}] LIMIT {1});", _LoggingTableName, Result.ToString()));
        }
        return Result;
    }

    public DataTable GetDataToSync(bool AllLogs, System.DateTime BeginDate, System.DateTime EndDate)
    {
        System.Data.Common.DbParameter[] vPar = {
			_Sql.NewParameter("BeginDate", DbType.Date, BeginDate),
			_Sql.NewParameter("EndDate", DbType.Date, EndDate)
		};
        string vSQLText = null;
        if (AllLogs == true)
        {
            vSQLText = string.Format("SELECT * FROM [{0}] WHERE ([Sent] = 0 AND [Time] BETWEEN @BeginDate AND @EndDate);", _LoggingTableName);
        }
        else
        {
            vSQLText = string.Format("SELECT * FROM [{0}] WHERE ([Sent]=0 AND [Time] BETWEEN @BeginDate AND @EndDate AND Category='Error');", _LoggingTableName);
        }
        System.Data.DataTable vResult = _Sql.Select(vSQLText, vPar);
        vResult.TableName = _LoggingTableName;
        return vResult;
    }

    public int SetDataSync(ref System.Data.DataTable LogTable)
    {
        int vResult = 0;
        string vSQLText = string.Format("UPDATE [{0}] SET [Sent]=@Sended WHERE [UID]=@UID;", _LoggingTableName);
        System.Data.Common.DbParameter[] vPar = {
			_Sql.NewParameter("Sended", DbType.Int32, System.Convert.ToInt32(1)),
			_Sql.NewParameter("UID", DbType.String)
		};
        foreach (System.Data.DataRow vRow in LogTable.Rows)
        {
            vPar[1].Value = (string)vRow["UID"];
            vResult += _Sql.Exec(vSQLText, vPar[0], vPar[1]);
        }
        return vResult;
    }

    public void Split(string NewFileName)
    {
        NewFileName = System.IO.Path.GetFileName(NewFileName);
        NewFileName = _WorkingFolder + "\\" + NewFileName;
        if (System.IO.File.Exists(NewFileName) == false)
        {
            Close();
            System.Threading.Thread.Sleep(300);
            System.IO.File.Move(_FullName, NewFileName);
            Init();
        }
    }

    public void Close()
    {
        _Started = false;
        while (_IsFinished == false)
        {
            if (_Sleeping == true)
            {
                _Started = false;
                _LogEvent.Set();
            }
            Thread.Sleep(100);
        }
        if (_InsertCmd != null)
        {
            _InsertCmd.Dispose();
            _InsertCmd = null;
        }
        if (_Sql != null)
        {
            _Sql.Close();
            _Sql = null;
        }
        if (_FileWriter != null)
        {
            _FileWriter.Close();
            _FileWriter = null;
        }
        _Th = null;
        _Initialized = false;
    }

    private void Writer()
    {
        _Started = true;
        _IsFinished = false;
        try
        {
            do
            {
                while (_Queue.Count > 0)
                {
                    var _with1 = _InsertCmd;
                    _with1.Parameters[0].Value = System.Guid.NewGuid();
                    //     ("UID", DbType.String
                    _with1.Parameters[1].Value = _Queue[0].DateTime;
                    //   "Time", DbType.DateTime
                    _with1.Parameters[2].Value = _Queue[0].LogType;
                    //       "Category", DbType.String))
                    _with1.Parameters[3].Value = _Queue[0].Class;
                    //         "Class", DbType.String
                    _with1.Parameters[4].Value = _Queue[0].Method;
                    //        "Function", DbType.String
                    _with1.Parameters[5].Value = _Queue[0].Description;
                    //   "Description", DbType.String
                    _with1.Parameters[6].Value = System.Convert.ToInt32(0);
                    //                 "Sent", DbType.Int32
                    //Dim vParameterIdx As Integer = 7
                    foreach (KeyValuePair<System.Data.DataColumn, object> vRecord in _Queue[0].StaticData)
                    {
                        _with1.Parameters[vRecord.Key.ColumnName].Value = vRecord.Value;
                        //vParameterIdx += 1
                    }
                    //RemoveBadCharacters(_InsertCmd)
                    try
                    {
                        _with1.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        //_FileWriter.Write(_Queue(0))
                        if (_Queue[0].LogTypeCode == System.Convert.ToInt32(enmLogType.Error))
                        {
                            _FileWriter.Write(_Queue[0]);
                        }
                        _FileWriter.Write(new SysLogData(this.GetType().FullName, "Writer", ex));
                    }
                    finally
                    {
                        _Queue.RemoveAt(0);
                    }
                }
                if (_Started == true)
                {
                    _LogEvent.Reset();
                    _Sleeping = true;
                    _LogEvent.WaitOne();
                    _Sleeping = false;
                }
            } while (_Started == true | _Queue.Count > 0);
        }
        catch (Exception Ex)
        {
            _FileWriter.Write(new SysLogData(this.GetType().Name, "Writer", Ex));
        }
        _IsFinished = true;
    }



    private void CreateLogTable(AppSql.SQL SqlClient, string TableName, DataColumn[] DataColumns)
    {
        string vStrSQL = "" + "CREATE TABLE IF NOT EXISTS [" + TableName + "]" + " ([UID] CHAR(36) PRIMARY KEY ASC NOT NULL," + " [Time] DATETIME NOT NULL," + " [Category] VARCHAR(20)," + " [Class] VARCHAR(50)," + " [Function] VARCHAR(50)," + " [Description] VARCHAR(4000)," + " [Sent] INTEGER";

        foreach (DataColumn vStaticColumn in DataColumns)
        {
            vStrSQL += ", [" + vStaticColumn.ColumnName + "] ";
            switch (vStaticColumn.DataType.Name.ToUpper())
            {
                case "STRING":
                    vStrSQL += "VARCHAR(" + vStaticColumn.MaxLength.ToString() + ")";
                    break;
                case "BYTE":
                case "INTEGER":
                case "INT16":
                case "INT32":
                case "INT64":
                    vStrSQL += "INTEGER";
                    break;
                case "DATE":
                case "DATETIME":
                    vStrSQL += "DATETIME";
                    break;
                case "SINGLE,DOUBLE,DECIMAL":
                    vStrSQL += "DECIMAL(18,2)";
                    break;
                default:
                    vStrSQL += "VARCHAR(4000)";
                    break;
            }
        }
        vStrSQL += ");";
        _Sql.Exec(vStrSQL);
    }

    private DbCommand GetInsertCommand(SQL SqlClient, DataColumn[] DataColumns)
    {

        string vStrStaticFieldNames = GetInsertFieldNames(DataColumns);
        if (vStrStaticFieldNames == null || vStrStaticFieldNames.Length < 1)
        {
            vStrStaticFieldNames = " ";
        }

        List<DbParameter> vParList = GetInsertParameters(SqlClient, DataColumns);

        DbCommand vInsertCmd = SqlClient.NewCommand("INSERT INTO " + _LoggingTableName + " (" + "[UID], " + "[Time], " + "[Category], " + "[Class], " + "[Function], " + "[Description], " + "[Sent]" + vStrStaticFieldNames.Replace("@", "") + ") VALUES (" + "@UID, " + "@Time, " + "@Category, " + "@Class, " + "@Function, " + "@Description, " + "@Sent" + vStrStaticFieldNames.Replace("[", "").Replace("]", "") + ");", vParList);
        return vInsertCmd;
    }

    private System.Collections.Generic.List<System.Data.Common.DbParameter> GetInsertParameters(AppSql.SQL SqlClient, DataColumn[] DataColumns)
    {

        List<DbParameter> vParList = new List<DbParameter>();
        vParList.Add(SqlClient.NewParameter("UID", DbType.String));
        vParList.Add(SqlClient.NewParameter("Time", DbType.DateTime));
        vParList.Add(SqlClient.NewParameter("Category", DbType.String));
        vParList.Add(SqlClient.NewParameter("Class", DbType.String));
        vParList.Add(SqlClient.NewParameter("Function", DbType.String));
        vParList.Add(SqlClient.NewParameter("Description", DbType.String));
        vParList.Add(SqlClient.NewParameter("Sent", DbType.Int32));

        if (DataColumns != null)
        {
            foreach (DataColumn vStaticColumn in DataColumns)
            {
                switch (vStaticColumn.DataType.Name.ToUpper())
                {
                    case "STRING":
                        vParList.Add(SqlClient.NewParameter(vStaticColumn.ColumnName, DbType.String));
                        break;
                    case "BYTE":
                    case "INTEGER":
                    case "INT16":
                    case "INT32":
                    case "INT64":
                        vParList.Add(SqlClient.NewParameter(vStaticColumn.ColumnName, DbType.Int64));
                        break;
                    case "DATE":
                    case "DATETIME":
                        vParList.Add(SqlClient.NewParameter(vStaticColumn.ColumnName, DbType.DateTime));
                        break;
                    case "SINGLE,DOUBLE,DECIMAL":
                        vParList.Add(SqlClient.NewParameter(vStaticColumn.ColumnName, DbType.Decimal));
                        break;
                    default:
                        vParList.Add(SqlClient.NewParameter(vStaticColumn.ColumnName, DbType.String));
                        break;
                }
            }
        }
        return vParList;
    }

    private string GetInsertFieldNames(DataColumn[] DataColumns)
    {
        string vStrStaticFiledNames = "";
        foreach (DataColumn vStaticColumn in DataColumns)
        {
            vStrStaticFiledNames += ", [@" + vStaticColumn.ColumnName + "] ";
        }
        return vStrStaticFiledNames;
    }



    private void RemoveBadCharacters(ref DbCommand Command)
    {
        const char FirstChar = ' ';
        if (Command != null)
        {
            foreach (DbParameter vPar in Command.Parameters)
            {
                if (vPar.DbType == DbType.String && vPar.Value is string && (vPar.Value != null))
                {
                    string vValue = vPar.Value.ToString();
                    int vIdx = 0;
                    StringBuilder vStrBuilder = new StringBuilder();
                    while (vIdx < vValue.Length)
                    {
                        if (vValue[vIdx] < FirstChar)
                        {
                            vStrBuilder.Append(FirstChar);
                        }
                        else
                        {
                            vStrBuilder.Append(vValue[vIdx]);
                        }
                        vIdx += 1;
                    }
                    vPar.Value = vStrBuilder.ToString();
                }
            }
        }
    }







}