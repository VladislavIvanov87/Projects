
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;




public class Log
{

	#region "AppSql Proxy methods"

    static Log()
    {
        AppSql.SQL.SqLiteAssembly = typeof(System.Data.SQLite.SQLiteCommand).Assembly;
    }

	public static System.Reflection.Assembly MsSqlAssembly {
		get { return AppSql.SQL.MsSqlAssembly; }
		set { AppSql.SQL.MsSqlAssembly = value; }
	}

	public static System.Reflection.Assembly SqLiteAssembly {
		get { return AppSql.SQL.SqLiteAssembly; }
		set { AppSql.SQL.SqLiteAssembly = value; }
	}


	private static string _LoggingTableName = "PDA_Log";
	public static string LoggingTableName {
		get { return _LoggingTableName; }
		set { _LoggingTableName = value; }
	}

	#endregion


	private static List<Log> _Instances = new List<Log>();

	private static Log _Current;
	public static Log Current {
		get { return _Current; }
		set { _Current = value; }
	}

	public static Log[] RunnedInstances {
		get { return _Instances.ToArray(); }
	}

	public static bool SeparateFileForEachTypeOfRecord = true;

	public static string FileNameDateFormat = "yyyy.MM.dd HH_mm_ss_f";

	private IWriter _Writer;
	private Dictionary<System.Data.DataColumn, object> _StaticData;
	private enmLogType _LogLevel;
	private enmLogType _eMailLevel;

	private EMail _eMail;



	public Log(bool SqLiteLog, string Param, Dictionary<DataColumn, object> StaticData)
	{
		_StaticData = StaticData;
		if (SqLiteLog == true) {
			_Writer = new SqLiteWriter(this, Param);
		} else {
			_Writer = new FileWriter(Param);
		}

		_LogLevel = enmLogType.UserAction;
		_eMailLevel = enmLogType.None;
		if (Environment.OSVersion.Platform != PlatformID.WinCE) {
			try {
				_eMail = new EMail(this);
			} catch (Exception ex) {
				LogData vErrData = new LogData(this.GetType().Name, "New", ex);
				Write(vErrData);
				_eMail = null;
			}
		} else {
			_eMail = null;
		}
		lock (_Instances) {
			_Instances.Add(this);
		}
		if (_Current == null) {
			_Current = this;
		}
	}

    private void Constructor(string ConnectionString, Dictionary<DataColumn, object> StaticData)
    {
        if (StaticData == null)
        {
            StaticData = new Dictionary<DataColumn, object>();
        }
        _StaticData = StaticData;
        _Writer = new MsSqlWriter(this, ConnectionString);

        _LogLevel = enmLogType.UserAction;
        _eMailLevel = enmLogType.None;
        if (Environment.OSVersion.Platform != PlatformID.WinCE)
        {
            try
            {
                _eMail = new EMail(this);
            }
            catch (Exception ex)
            {
                SysLogData vErrData = new SysLogData(this.GetType().Name, "New", ex);
                Write(vErrData);
                _eMail = null;
            }
        }
        else
        {
            _eMail = null;
        }

        lock (_Instances)
        {
            _Instances.Add(this);
        }
        if (_Current == null)
        {
            _Current = this;
        }
    }

	public  Log(string ConnectionString, Dictionary<DataColumn, object> StaticData )
	{
        Constructor(ConnectionString, StaticData);
	}
    public  Log(string ConnectionString)
    {
        Constructor(ConnectionString, null);
    }

	public static string[] DebugCompiledList()
	{

		int vInitialized = 0;

		#if DEBUG
		vInitialized = 1;
		#endif

		List<string> lst = new List<string>();
		if (lst.Contains("AppSql.dll") == false) {
			lst.AddRange(AppSql.SQL.DebugCompiledList());
		}
		if (vInitialized == 1) {
			lst.Add("appLog.dll");
		}

		return lst.ToArray();
	}



	public enmLogType StringToEnmLogType(string Setting, bool ThrowIfError)
	{
		switch (Setting.Trim().ToUpper()) {
			case "USERACTION":
				return enmLogType.UserAction;
			case "ACTION":
				return enmLogType.Action;
			case "WARNING":
				return enmLogType.Warning;
			case "ERROR":
				return enmLogType.Error;
			case "NONE":
				return enmLogType.None;
			default:
				if (ThrowIfError == true) {
					throw new System.Exception("Cant convert '" + Setting + "' to enmLogType");
				} else {
					SysLogData vData = new SysLogData(this.GetType().Name, "StringToEnmLogType", new System.Exception("Cant convert '" + Setting + "' to enmLogType"));
					Write(vData);
					return enmLogType.Error;
				}
				
		}
	}

	public static enmLogType StringToEnmLogType(string Setting)
	{
		switch (Setting.Trim().ToUpper()) {
			case "USERACTION":
				return enmLogType.UserAction;
			case "ACTION":
				return enmLogType.Action;
			case "WARNING":
				return enmLogType.Warning;
			case "ERROR":
				return enmLogType.Error;
			case "NONE":
				return enmLogType.None;
			default:
                return enmLogType.None;
		}
	}

	public enmLogType LogLevel {
		get { return _LogLevel; }
		set { _LogLevel = value; }
	}

	public enmLogType eMailLevel {
		get { return _eMailLevel; }
		set {
			if (Environment.OSVersion.Platform == PlatformID.WinCE) {
				SysLogData vErrData = new SysLogData(this.GetType().Name, "set_eMailLevel", new System.Exception("eMail log is not supported on Windows CE platform!"));
				Write(vErrData);
			} else if (_eMail == null) {
				SysLogData vErrData = new SysLogData(this.GetType().Name, "set_eMailLevel", new System.Exception("eMail log instance can't be created!"));
				Write(vErrData);
			} else {
				_eMailLevel = value;
			}
		}
	}

	public object  this[DataColumn  Field] {
		get { return  _StaticData[Field]; }
		set { _StaticData[Field] = value; }
	}

	public IWriter Writer {
		get { return _Writer; }
	}

	public EMail eMail {
		get { return _eMail; }
	}

	public void Write(LogData Data)
	{
		WriteWithoutEmail(Data);
		if (!(Data.LogTypeCode < Convert.ToInt32(_eMailLevel)) && _eMail != null) {
			Data.StaticData = _StaticData;
			_eMail.Write(Data);
		}
	}

	public void UserAction(string InClass, string InMethod, string Message)
	{
		LogData vData = new LogData(InClass, InMethod, Message, true);
		Write(vData);
	}

	public void Action(string InClass, string InMethod, string Message)
	{
		LogData vData = new LogData(InClass, InMethod, Message, false);
		Write(vData);
	}

	public void Warning(string InClass, string InMethod, string Message, enmLogType Type)
	{
		LogData vData = new LogData(InClass, InMethod, Message, enmLogType.Warning);
		Write(vData);
	}

	public void Error(string InClass, string InMethod, System.Exception exception)
	{
		LogData vData = new LogData(InClass, InMethod, exception);
		Write(vData);
	}

	public void Debug(string InClass, string InMethod, string Message, enmLogType Type)
	{
		LogData vData = new LogData(InClass, InMethod, Message, enmLogType.Debug);
		Write(vData);
	}

	public void Exception(System.Exception exception)
	{
		if (exception.InnerException != null) {
			this.Exception(exception.InnerException);
		}
		appLogException vCustomException = appLogException.Convert(exception);
		string vStrClass =  vCustomException["Class"];
		string vStrMethod = vCustomException["Method"];
		LogData vData = new LogData(vStrClass, vStrMethod, vCustomException);
		Write(vData);
	}

	/// <summary>
	/// New File name used to rename Log.Db. If file already exist Split will not be performed
	/// </summary>
	/// <param name="ArchiveFileName"></param>
	/// <remarks></remarks>
	public void Split(string ArchiveFileName)
	{
		_Writer.Split(ArchiveFileName);
	}

	public void Close()
	{
		if (_eMail != null) {
			_eMail.Close();
			_eMail = null;
		}
		if (_Writer != null) {
			_Writer.Close();
			_Writer = null;
		}

		lock (_Instances) {
			_Instances.Remove(this);
		}
		if (object.ReferenceEquals(_Current, this)) {
			_Current = null;
		}
	}

	internal void WriteWithoutEmail(LogData Data)
	{
		if (!(Data.LogTypeCode < Convert.ToInt32(_LogLevel))) {
			Data.StaticData = _StaticData;
			_Writer.Write(Data);
		}
	}

	internal System.Data.DataColumn[] StaticDataColumns {
		get {
			if (_StaticData == null) {
				return new DataColumn[] {};
				// Returns an empty array of datacolumn
			} else {
                DataColumn[] columns = new DataColumn[_StaticData.Keys.Count] ;
                _StaticData.Keys.CopyTo(columns,0);
				return  columns;
			}
		}
	}



}

