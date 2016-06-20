
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

public class EMail
{


    private string _From;
    private string _To;
    private string[] _arrTo;
    private string _Subject;
    private List<LogData> _Queue;
    private string _Host;
    private int _Port;
    private ManualResetEvent _LogEvent;
    private bool _Started;
    private bool _Finished;
    private Log _Log;

    private Action<MailData> _eMailSender;

    public EMail( Log log)
    {
        _Log = log;
        _Queue = new List<LogData>();
        _LogEvent = new ManualResetEvent(false);
        _Subject = "$EVENTTYPE$>$CLASS$>$METHOD$";
        Thread t = new Thread(new ThreadStart(this.Send));
        t.Name = this.GetType().FullName + ".Send";
        t.Start();
    }

    public Action<MailData> eMailSender
    {
        get { return _eMailSender; }
        set { _eMailSender = value; }
    }

    public string From
    {
        get { return _From; }
        set
        {
            if (value == null)
                value = "";
            _From = value;
        }
    }

    public string To
    {
        get
        {
            string vResult = "";
            if (_arrTo != null)
            {
                foreach (string vStr in _arrTo)
                {
                    vResult += "; " + vStr;
                }
                if (vResult.Length > 0)
                    vResult = vResult.Substring(2);
            }
            return vResult;
        }
        set
        {
            if (value == null)
                value = "";
            _To = value;
            List<string> vArrTo = new List<string>();
            foreach (string vStr in _To.Split(',', ';'))
            {
                if (vStr.Trim().Length > 0)
                    vArrTo.Add(vStr);
            }
            _arrTo = vArrTo.ToArray();
        }
    }

    public string Subject
    {
        get { return _Subject; }
        set
        {
            if (value == null)
                value = "";
            _Subject = value;
        }
    }

    public string Host
    {
        get { return _Host; }
        set
        {
            try
            {
                if (value == null)
                    value = "";
                int vDelimeterIdx = value.LastIndexOf(Convert.ToChar(":"));
                if (vDelimeterIdx > 0)
                {
                    Port = int.Parse(value.Substring(1 + vDelimeterIdx));
                    _Host = value.Substring(0, vDelimeterIdx);
                }
                else
                {
                    _Host = value;
                }
            }
            catch (Exception ex)
            {
                _Log.WriteWithoutEmail(new LogData(this.GetType().Name, "set_Host", new Exception("Value=" + value + ". " + ex.Message)));
            }

        }
    }

    public int Port
    {
        get { return _Port; }
        set { _Port = value; }
    }


    public void Write(LogData Data)
    {
        _Queue.Add(Data);
        _LogEvent.Set();
    }

    private void Send()
    {
        _Started = true;
        _Finished = false;
        try
        {
            do
            {
                while (_Queue.Count > 0)
                {
                    try
                    {
                        if (_eMailSender != null)
                        {
                            System.Text.StringBuilder vStrBuilder = new System.Text.StringBuilder();
                            vStrBuilder.AppendLine("===");
                            vStrBuilder.AppendLine("UID=" + System.Guid.NewGuid().ToString());
                            //     ("UID", DbType.String
                            vStrBuilder.AppendLine("Time=" + String.Format("'{0:yyyy-MM-dd HH:mm:ss}'",_Queue[0].DateTime));
                            //   "Time", DbType.DateTime
                            vStrBuilder.AppendLine("LogType=" + _Queue[0].LogType);
                            //       "Category", DbType.String))
                            vStrBuilder.AppendLine("Class=" + _Queue[0].Class);
                            //         "Class", DbType.String
                            vStrBuilder.AppendLine("Method=" + _Queue[0].Method);
                            //        "Function", DbType.String
                            vStrBuilder.AppendLine("Description=" + _Queue[0].Description);
                            //   "Description", DbType.String
                            vStrBuilder.AppendLine("Sent=0");
                            //                 "Sent", DbType.Int32
                            foreach (System.Collections.Generic.KeyValuePair<DataColumn, object> vKeyValuePair in _Queue[0].StaticData)
                            {
                                vStrBuilder.AppendLine(vKeyValuePair.Key.ColumnName + "=" + vKeyValuePair.Value.ToString());
                            }
                            MailData vMessageData = new MailData();
                            var _with1 = vMessageData;
                            _with1.From = _From;
                            _with1.To = _arrTo;
                            _with1.Host = _Host;
                            _with1.Port = _Port;
                            _with1.Subject = _Subject.Replace("$EVENTTYPE$", _Queue[0].LogType).Replace("$CLASS$", _Queue[0].Class).Replace("$METHOD$", _Queue[0].Method);
                            _with1.Body = vStrBuilder.ToString();
                            vStrBuilder = null;
                            _eMailSender(vMessageData);
                        }
                    }
                    catch (Exception Ex)
                    {
                        LogData vErrData = new LogData(this.GetType().Name, "Send", Ex);
                        _Log.WriteWithoutEmail(vErrData);
                    }
                    _Queue.RemoveAt(0);
                    Thread.Sleep(200);
                }
                _LogEvent.Reset();
                _LogEvent.WaitOne();
            } while (_Started == true);

        }
        catch (Exception ex)
        {
            LogData vErrData = new LogData(this.GetType().Name, "Send", ex);
            _Log.WriteWithoutEmail(vErrData);
            _Started = false;
        }
        _Finished = true;
    }

    public void Close()
    {
        _Started = false;
        while ((false == _Finished))
        {
            _LogEvent.Set();
            Thread.Sleep(200);
        }
    }



}


public struct MailData
{

    private string _From;
    private string[] _To;
    private string _Host;
    private int _Port;
    private string _Subject;

    private string _Body;
    public string From
    {
        get { return _From; }
        set
        {
            if (value == null)
                value = "";
            _From = value;
        }
    }

    public string[] To
    {
        get { return _To; }
        set { _To = value; }
    }

    public string Host
    {
        get { return _Host; }
        set
        {
            if (value == null)
                value = "";
            _Host = value;
        }
    }

    public int Port
    {
        get { return _Port; }
        set { _Port = value; }
    }

    public string Subject
    {
        get { return _Subject; }
        set
        {
            if (value == null)
                value = "";
            _Subject = value;
        }
    }

    public string Body
    {
        get { return _Body; }
        set
        {
            if (value == null)
                value = "";
            _Body = value;
        }
    }

}

