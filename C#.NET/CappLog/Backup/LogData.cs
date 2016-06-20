
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;



public class LogData
{

    private enmLogType _LogType;
    private System.DateTime _DateTime;
    private string _Class;
    private string _Method;
    private string _Description;
    private string _Message;
    private System.Exception _Exception;

    private Dictionary<System.Data.DataColumn, object> _StaticData;

    public LogData(string InClass, string InMethod, string Description, bool UserAction)
    {
        if (UserAction == true)
        {
            this._LogType = enmLogType.UserAction;
        }
        else
        {
            this._LogType = enmLogType.Action;
        }
        this._DateTime = System.DateTime.Now ;

        this._Class = InClass;
        this._Method = InMethod;
        this._Description = Description;
        this.IsSystem = false;

    }


    public LogData(string InClass, string InMethod, string Description, enmLogType Type)
    {
        this._LogType = Type;
        this._DateTime = System.DateTime.Now;

        this._Class = InClass;
        this._Method = InMethod;
        this._Description = Description;
        this.IsSystem = false;

    }


    public LogData(string InClass, string InMethod, System.Exception Exception)
    {
        this._LogType = enmLogType.Error;
        this._DateTime = System.DateTime.Now;

        this._Class = InClass;
        this._Method = InMethod;
        this._Exception = Exception;
        this.IsSystem = false;

    }

    public string LogType
    {
        get { return _LogType.ToString(); }
    }

    public int LogTypeCode
    {
        get { return Convert.ToInt32(_LogType); }
    }

    public System.DateTime DateTime
    {
        get { return _DateTime; }
    }

    public string Class
    {
        get { return _Class; }
    }

    public string Method
    {
        get { return _Method; }
    }

    public string Description
    {
        get
        {
            if (_Exception == null)
            {
                return _Description;
            }
            else
            {
                return _Exception.Message;
            }
        }
    }

    public Dictionary<System.Data.DataColumn, object> StaticData
    {
        get
        {
            if (_StaticData == null)
            {
                return new Dictionary<System.Data.DataColumn, object>();
            }
            else
            {
                return _StaticData;
            }
        }
        internal set { _StaticData = value; }
    }



    protected bool IsSystem;
    /// <summary>
    /// Determine is log created from Log system or client system. True if it is from Log system
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
    internal bool IsLogSystem
    {
        get { return IsSystem; }
    }

}


internal class SysLogData : LogData
{

    private SysLogData(string InClass, string InMethod, string Description, bool UserAction)
        : base(InClass, InMethod, Description, UserAction)
    {
    }
    public SysLogData(string InClass, string InMethod, string Description, enmLogType Type)
        : base(InClass, InMethod, Description, Type)
    {
        IsSystem = true;
    }

    public SysLogData(string InClass, string InMethod, System.Exception Exception)
        : base(InClass, InMethod, Exception)
    {
        IsSystem = true;
    }


}

