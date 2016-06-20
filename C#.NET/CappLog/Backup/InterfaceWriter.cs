
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
public interface IWriter
{

    void Write(LogData Data);
    int Shrink(int RecordsToSave);
    DataTable GetDataToSync(bool AllLogs, System.DateTime BeginDate, System.DateTime EndDate);
    int SetDataSync(ref System.Data.DataTable LogTable);

    string WorkingFolder { get; }
    void Split(string ArchiveFileName);
    void Close();
}
