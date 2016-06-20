using System;
using System.Collections.Generic;
using System.Data;

public class LogData
{
    private bool isSystem;
    private LogType logType;
    private DateTime logTime;
    private string classs;
    private string method;
    private string description;
    private Exception exception;
    
    private Dictionary<DataColumn, object> staticData;

    public LogData(string inClass, string inMethod, string description, bool userAction)
    {
        if (userAction == true)
        {
            this.logType = global::LogType.UserAction;
        }
        else
        {
            this.logType = global::LogType.Action;
        }

        this.logTime = DateTime.Now;

        this.classs = inClass;
        this.method = inMethod;
        this.description = description;
        this.IsSystem = false;
    }

    public LogData(string inClass, string inMethod, string description, LogType type)
    {
        this.logType = type;
        this.logTime = DateTime.Now;
        this.classs = inClass;
        this.method = inMethod;
        this.description = description;
        this.IsSystem = false;
    }

    public LogData(string inClass, string inMethod, Exception exception)
    {
        this.logType = global::LogType.Error;
        this.logTime = DateTime.Now;
        this.classs = inClass;
        this.method = inMethod;
        this.exception = exception;
        this.IsSystem = false;
    }

    public bool IsSystem
    {
        get
        {
            return this.isSystem;
        }

        protected set
        {
            this.isSystem = value;
        }
    }

    public string LogType
    {
        get { return this.logType.ToString(); }
    }

    public int LogTypeCode
    {
        get { return Convert.ToInt32(this.logType); }
    }

    public DateTime LogTime
    {
        get { return this.logTime; }
    }

    public string Class
    {
        get { return this.classs; }
    }

    public string Method
    {
        get { return this.method; }
    }

    public string Description
    {
        get
        {
            if (this.exception == null)
            {
                return this.description;
            }
            else
            {
                return this.exception.Message;
            }
        }
    }

    public Dictionary<DataColumn, object> StaticData
    {
        get
        {
            if (this.staticData == null)
            {
                return new Dictionary<DataColumn, object>();
            }
            else
            {
                return this.staticData;
            }
        }

        internal set
        {
            this.staticData = value;
        }
    }

    internal bool IsLogSystem
    {
        get { return this.IsSystem; }
    }
}