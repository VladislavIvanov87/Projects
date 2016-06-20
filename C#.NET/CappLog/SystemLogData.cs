namespace CappLog
{
    using System;

    public class SystemLogData : LogData
    {   
        public SystemLogData(string inClass, string inMethod, string description, LogType type)
            : base(inClass, inMethod, description, type)
        {
            this.IsSystem = true;
        }

        public SystemLogData(string inClass, string inMethod, Exception exception)
            : base(inClass, inMethod, exception)
        {
            this.IsSystem = true;
        }

        private SystemLogData(string inClass, string inMethod, string description, bool userAction)
            : base(inClass, inMethod, description, userAction)
        {
        }
    }
}
