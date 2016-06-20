
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
internal class FileWriter : IWriter
{


    private List<LogData> _Queue;
    private ManualResetEvent _LogEvent;
    private bool _Sleeping;
    private bool _Started;
    private bool _Finished;

    private string _WorkingFolder;

    public FileWriter(string Path)
    {
        _WorkingFolder = Path;
        if (System.IO.Directory.Exists(_WorkingFolder) == false)
        {
            System.IO.Directory.CreateDirectory(_WorkingFolder);
        }
        _Queue = new List<LogData>();
        _LogEvent = new ManualResetEvent(false);
        Thread t = new Thread(new ThreadStart(this.Writer));
        t.Name  = this.GetType().FullName  + ".Writer";
        t.Start();
    }

    public string WorkingFolder
    {
        get { return _WorkingFolder; }
    }

    public void Close()
    {
        _Started = false;
        //If _Sleeping = True Then
        //    Call _LogEvent.Set()
        //End If
        while (_Finished == false)
        {
            _LogEvent.Set();
            Thread.Sleep(100);
        }
    }

    public void Write(LogData Data)
    {
        _Queue.Add(Data);
        if (_Sleeping == true)
        {
            _LogEvent.Set();
        }
    }

    private void Writer()
    {
        _Started = true;
        _Finished = false;
        System.Text.StringBuilder vStrBuilder = new System.Text.StringBuilder();
        do
        {
            while (_Queue.Count > 0)
            {
                vStrBuilder.Remove(0, vStrBuilder.Length);
                vStrBuilder.AppendLine("");
                vStrBuilder.Append(String.Format("{0:yyyy-MM-dd HH:mm:ss}", _Queue[0].DateTime) + " ");
                //   "Time", DbType.DateTime
                vStrBuilder.Append(_Queue[0].LogType + " ");
                //       "Category", DbType.String))
                vStrBuilder.Append(_Queue[0].Class + ".");
                //         "Class", DbType.String
                vStrBuilder.AppendLine(_Queue[0].Method);
                //        "Function", DbType.String
                vStrBuilder.AppendLine("\t" + _Queue[0].Description);
                //   "Description", DbType.String
                //vStrBuilder.AppendLine("Sent=0") '                 "Sent", DbType.Int32

                foreach (System.Collections.Generic.KeyValuePair<DataColumn, object> vKeyValuePair in _Queue[0].StaticData)
                {
                    vStrBuilder.AppendLine("\t" + vKeyValuePair.Key.ColumnName.ToString() + "=" + vKeyValuePair.Value.ToString());
                }

                string vFileName = null;

                if (Log.SeparateFileForEachTypeOfRecord == true)
                {
                    vFileName = string.Format("{0}\\{1}_{2}.Log", _WorkingFolder, _Queue[0].LogType, String.Format("{0:" + Log.FileNameDateFormat + "}", _Queue[0].DateTime));
                }
                else
                {
                    vFileName = string.Format("{0}\\{1}.Log", _WorkingFolder, String.Format("{0:" + Log.FileNameDateFormat + "}", _Queue[0].DateTime));
                }

                //End If
                System.IO.StreamWriter vStreamWriter = null;
                //Loop if someone else has got exclusive access to file
                do
                {
                    try
                    {
                        vStreamWriter = new System.IO.StreamWriter(vFileName, true, System.Text.Encoding.UTF8);
                    }
                    catch 
                    {
                        vStreamWriter = null;
                    }
                } while (vStreamWriter == null);
                vStreamWriter.Write(vStrBuilder.ToString());
                vStreamWriter.Close();
                vStreamWriter.Dispose();
                vStreamWriter = null;
                _Queue.RemoveAt(0);
            }
            if (_Started == true)
            {
                _LogEvent.Reset();
                _Sleeping = true;
                _LogEvent.WaitOne();
                _Sleeping = false;
            }
        } while (_Started == true | _Queue.Count > 0);
        _Finished = true;
    }

    public System.Data.DataTable GetDataToSync(bool AllLogs, System.DateTime BeginDate, System.DateTime EndDate)
    {
        return null;
    }

    public int SetDataSync(ref System.Data.DataTable LogTable)
    {
        return -1;
    }

    public int Shrink(int RecordsToSave)
    {
        return -1;
    }

    public void Split(string NewFileName)
    {
        throw new System.Exception(this.GetType().FullName + " does not support split functionality!");
    }
}
