using System;
using System.Data;

public interface IWriter
{
    string WorkingFolder { get; }

    int Shrink(int recordsToSave);

    int SetDataSync(ref DataTable logTable);

    DataTable GetDataToSync(bool allLogs, DateTime beginDate, DateTime endDate);

    void Write(LogData data);

    void Split(string archiveFileName);

    void Close();
}
