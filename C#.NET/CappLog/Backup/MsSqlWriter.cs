
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

internal class MsSqlWriter : IWriter
{

    private string _LoggingTableName;
    private bool _Initialized;
    private Log _Log;
    private string _ConnectionString;
    private SQL _Sql;
    private FileWriter _FileWriter;
    private DbCommand _InsertCmd;
    private List<LogData> _Queue;
    private ManualResetEvent _LogEvent;
    private System.Threading.Thread _Th;
    //Private _Status As Status
    private bool _Sleeping;
    private bool _Started;
    private bool _IsFinished;

    private string _WorkingFolder;

    private ManualResetEvent _PauseEvent;

    public MsSqlWriter(Log ObjLog, string ConnectionString)
    {
        if (Environment.OSVersion.Platform == PlatformID.WinCE)
        {
            throw new appLogException(this.GetType().FullName, "New(appLog.Log, String)", "Unsupported platform!", ConnectionString);
        }
        _Log = ObjLog;
        _LoggingTableName = Log.LoggingTableName;
        _ConnectionString = ConnectionString;
        _WorkingFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        if (_WorkingFolder.ToUpper().StartsWith("FILE:/") == true || _WorkingFolder.ToUpper().StartsWith("FILE:\\") == true)
            _WorkingFolder = _WorkingFolder.Substring(6);
        _PauseEvent = new ManualResetEvent(false);
        //_Status = New Status
        Init();
    }

    private void Init()
    {
        _Initialized = false;
        if (_FileWriter == null)
        {
            _FileWriter = new FileWriter(_WorkingFolder);
        }
        try
        {
            _Sql = new SQL(SQLClientType.MSSQL);
            _Sql.ConnectionString = _ConnectionString;

            //Check database for errors and recreate Db if get reoubles with connection
            if (_Sql.DbObjectExist(enmDbObjectType.Table, _LoggingTableName) == false)
            {
                CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns);
            }
            Action<SqlException> vCurrentErrorHandler = _Sql.ErrorHandler;
            _Sql.ErrorHandler = null;
            try
            {
                string vStrSqlSelect = string.Format("SELECT COUNT(*) FROM [{0}] ;", _LoggingTableName);
                _Sql.Value(vStrSqlSelect);
            }
            catch 
            {
                _Sql.Connection.Close();
                _Sql.ConnectionString = _ConnectionString;
                CreateLogTable(_Sql, _LoggingTableName, _Log.StaticDataColumns);
            }
            finally
            {
                _Sql.ErrorHandler = vCurrentErrorHandler;
            }

            if (_InsertCmd != null)
            {
                _InsertCmd.Dispose();
                _InsertCmd = null;
            }

            _InsertCmd = GetInsertCommand(_Sql, _Log.StaticDataColumns);

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


    //Private Sub Init()
    //    _Initialized = False
    //    If _FileWriter Is Nothing Then
    //        _FileWriter = New FileWriter(_WorkingFolder)
    //    End If
    //    Try
    //        _Sql = New SQL(SQLClientType.MSSQL)
    //        _Sql.ConnectionString = _ConnectionString
    //        Dim vParList As New List(Of DbParameter)
    //        vParList.Add(_Sql.NewParameter("UID", DbType.String))
    //        vParList.Add(_Sql.NewParameter("Time", DbType.DateTime))
    //        vParList.Add(_Sql.NewParameter("Category", DbType.String))
    //        vParList.Add(_Sql.NewParameter("Class", DbType.String))
    //        vParList.Add(_Sql.NewParameter("Function", DbType.String))
    //        vParList.Add(_Sql.NewParameter("Description", DbType.String))
    //        vParList.Add(_Sql.NewParameter("Sent", DbType.Int32))
    //        Dim vStrSQL As String = "" & _
    //                "CREATE TABLE IF NOT EXISTS " & _LoggingTableName & _
    //                " ([UID] uniqueidentifier PRIMARY KEY NOT NULL," & _
    //                " [Time] datetime NOT NULL," & _
    //                " [Category] VARCHAR(20)," & _
    //                " [Class] VARCHAR(50)," & _
    //                " [Function] VARCHAR(50)," & _
    //                " [Description] VARCHAR(4000)," & _
    //                " [Sent] INT"
    //        Dim vStrStaticFiledNames As String = ""
    //        For Each vStaticColumn As DataColumn In _Log.StaticDataColumns
    //            vStrStaticFiledNames &= ", [@" & vStaticColumn.ColumnName & "] "
    //            vStrSQL &= ", [" & vStaticColumn.ColumnName & "] "
    //            Select Case vStaticColumn.DataType.Name.ToUpper
    //                Case "STRING"
    //                    vStrSQL &= "VARCHAR(" & vStaticColumn.MaxLength.ToString & ")"
    //                    vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.String))
    //                Case "BYTE", "INTEGER", "INT16", "INT32", "INT64"
    //                    vStrSQL &= "INT"
    //                    vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.Int64))
    //                Case "DATE", "DATETIME"
    //                    vStrSQL &= "DATETIME"
    //                    vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.DateTime))
    //                Case "SINGLE,DOUBLE,DECIMAL"
    //                    vStrSQL &= "DECIMAL(18,2)"
    //                    vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.Decimal))
    //                Case Else
    //                    vStrSQL &= "VARCHAR(4000)"
    //                    vParList.Add(_Sql.NewParameter(vStaticColumn.ColumnName, DbType.String))
    //            End Select
    //        Next
    //        vStrSQL &= ");"
    //        Call _Sql.Exec(vStrSQL)

    //        If _InsertCmd IsNot Nothing Then
    //            _InsertCmd.Dispose()
    //            _InsertCmd = Nothing
    //        End If

    //        _InsertCmd = _Sql.NewCommand("INSERT INTO [" & _LoggingTableName & "] (" & _
    //                                        "[UID], " & _
    //                                        "[Time], " & _
    //                                        "[Category], " & _
    //                                        "[Class], " & _
    //                                        "[Function], " & _
    //                                        "[Description], " & _
    //                                        "[Sent]" & _
    //                                        vStrStaticFiledNames.Replace("@", "") & _
    //                                    ") VALUES (" & _
    //                                        "@UID, " & _
    //                                        "@Time, " & _
    //                                        "@Category, " & _
    //                                        "@Class, " & _
    //                                        "@Function, " & _
    //                                        "@Description, " & _
    //                                        "@Sent" & _
    //                                        vStrStaticFiledNames.Replace("[", "").Replace("]", "") & _
    //                                    ");", vParList)
    //        If _Queue Is Nothing Then
    //            _Queue = New List(Of LogData)
    //        End If
    //        If _LogEvent Is Nothing Then
    //            _LogEvent = New ManualResetEvent(False)
    //        End If
    //        If _Th Is Nothing Then
    //            _Th = New Thread(New ThreadStart(AddressOf Me.Writer))
    //            _Th.Start()
    //        End If
    //        _Initialized = True
    //    Catch Ex As Exception
    //        _FileWriter.Write(New SysLogData(Me.GetType.FullName, "New", Ex))
    //        _Initialized = False
    //    End Try
    //End Sub

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
            vPar[1].Value = vRow["UID"].ToString();
            vResult += _Sql.Exec(vSQLText, vPar[0], vPar[1]);
        }
        return vResult;
    }

    public void Split(string NewFileName)
    {
        throw new appLogException(this.GetType().FullName, "Split", "This class does not support Split functionality!");
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
        _InsertCmd.Dispose();
        _InsertCmd = null;
        _Sql.Close();
        _FileWriter.Close();
        _FileWriter = null;
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
                    _with1.Parameters[0].Value = System.Guid.NewGuid().ToString();
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
        string vStrSQL = "" + "IF  NOT EXISTS (SELECT * FROM [" + TableName + "])" + " CREATE TABLE [" + TableName + "]" + " ([UID] CHAR(36) PRIMARY KEY NOT NULL," + " [Time] DATETIME NOT NULL," + " [Category] VARCHAR(20)," + " [Class] VARCHAR(50)," + " [Function] VARCHAR(50)," + " [Description] VARCHAR(4000)," + " [Sent] INTEGER";

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

