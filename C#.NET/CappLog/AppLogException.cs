using System;
using System.Collections.Generic;
using System.Text;

internal class AppLogException : Exception
{
    private const char FieldSeparator = '☺';

    private Dictionary<string, string> fields;

    private Exception logException;

    public AppLogException(string inClass, string inMethod, string message, params object[] args)
    {
        if (IsCustomExceptionMessage(message) == true)
        {
            this.InnerException = new Exception(message);
            this.fields = new Dictionary<string, string>();
            this["Message"] = "Inner exception occured!";
            this["Class"] = inClass;
            this["Method"] = inMethod;
            this["Args"] = GetArgumentsString(args);
        }
        else
        {
            this.InnerException = null;
            this.fields = new Dictionary<string, string>();
            this["Message"] = message;
            this["Class"] = inClass;
            this["Method"] = inMethod;
            this["Args"] = GetArgumentsString(args);
        }
    }

    public AppLogException(string inClass, string inMethod, Exception exception, params object[] args)
    {
        this.InnerException = exception.InnerException;
        if (IsCustomExceptionMessage(exception.Message) == true)
        {
            this.fields = new Dictionary<string, string>();
            this["Message"] = "Inner exception occured!";
            this["Class"] = inClass;
            this["Method"] = inMethod;
            this["Args"] = GetArgumentsString(args);
        }
        else
        {
            this.fields = new Dictionary<string, string>();
            this["Message"] = exception.Message;
            this["Class"] = inClass;
            this["Method"] = inMethod;
            this["Args"] = GetArgumentsString(args);
        }
    }

    protected AppLogException(string message)
    {
        this.fields = ParseFields(message);
        this.InnerException = null;
    }

    protected AppLogException(string message, Exception innerException)
    {
        this.fields = ParseFields(message);
        innerException = this.logException;
    }

    protected AppLogException(Exception exception)
    {
        this.fields = ParseFields(exception.Message);
        this.InnerException = exception.InnerException;
    }

    public string this[string fieldName]
    {
        get
        {
            if (fieldName == null)
            {
                fieldName = string.Empty;
            }

            if (fieldName.Contains("=") == true)
            {
                fieldName = fieldName.Replace("=", string.Empty);
            }

            if (this.fields.ContainsKey(fieldName) == true)
            {
                return this.fields[fieldName];
            }
            else
            {
                return string.Empty;
            }
        }

        set
        {
            if (fieldName == null)
            {
                fieldName = string.Empty;
            }

            if (fieldName.Contains("=") == true)
            {
                fieldName = fieldName.Replace("=", string.Empty);
            }

            if (value == null)
            {
                value = string.Empty;
            }

            if (this.fields.ContainsKey(fieldName) == true)
            {
                this.fields[fieldName] = value;
            }
            else
            {
                this.fields.Add(fieldName, value);
            }
        }
    }

    public static bool IsCustomException(Exception message)
    {
        bool result = false;
        if (message.Message != null && message.Message.StartsWith(FieldSeparator.ToString()) == true)
        {
            result = true;
        }

        return result;
    }

    public static AppLogException Convert(Exception exception)
    {
        return new AppLogException(exception);
    }

    public Exception BaseException()
    {
        return new Exception(this["Message"], this.InnerException);
    }

    public override sealed string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (KeyValuePair<string, string> keyValuePair in this.fields)
        {
            stringBuilder.Append(string.Format("{0}{1}={2}", FieldSeparator, keyValuePair.Key, keyValuePair.Value));
        }

        return stringBuilder.ToString();
    }

    // public static bool IsCustomExceptionMessage
    // {
    //    get { return Message != null && Message.StartsWith(FieldSeparator) == true; }
    // }
    public virtual bool IsFullySpecified
    {
        get { return this.fields.ContainsKey("Message") && this.fields.ContainsKey("Class") && this.fields.ContainsKey("Method"); }
    }

    public override string Message
    {
        get { return this.ToString(); }
    }

    public new Exception InnerException
    {
        get { return this.InnerException; }
        set { this.InnerException = value; }
    }

    protected static bool IsCustomExceptionMessage(string message)
    {
        bool result = false;
        if (message != null && message.StartsWith(FieldSeparator.ToString()))
        {
            result = true;
        }

        return result;
    }

    // public static bool IsCustomException
    // {
    //    get { return Exception.Message != null && Exception.Message.StartsWith(FieldSeparator) == true; }
    // }
    protected static Dictionary<string, string> ParseFields(string message)
    {
        Dictionary<string, string> dictionaryResult = new Dictionary<string, string>();
        if (message != null && message.StartsWith(FieldSeparator.ToString()) == true && message.Length > 1)
        {
            message = message.Substring(1);
            string[] stringElements = message.Split(FieldSeparator);
            int equalSymbolIndex = 0;
            foreach (string stringElement in stringElements)
            {
                if (stringElement.Trim().Length > 0)
                {
                    equalSymbolIndex = stringElement.IndexOf('=');
                    if (equalSymbolIndex > 0)
                    {
                        string stringKey = stringElement.Substring(0, equalSymbolIndex);
                        if (stringKey == null)
                        {
                            stringKey = string.Empty;
                        }

                        string stringValue = stringElement.Substring(1 + equalSymbolIndex);
                        if (stringValue == null)
                        {
                            stringValue = string.Empty;
                        }

                        if (dictionaryResult.ContainsKey(stringKey) == true)
                        {
                            dictionaryResult[stringKey] = stringValue;
                        }
                        else
                        {
                            dictionaryResult.Add(stringKey, stringValue);
                        }
                    }
                }
            }

            stringElements = null;
        }
        else
        {
            dictionaryResult.Add("Message", message);
        }

        if (dictionaryResult.ContainsKey("Message") == false)
        {
            dictionaryResult.Add("Message", string.Empty);
        }

        return dictionaryResult;
    }

    protected static string GetArgumentsString(object[] args)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (args != null)
        {
            int argIndex = 0;
            while (argIndex < args.Length)
            {
                string stringArg = null;
                if (args[argIndex] == null)
                {
                    stringArg = "Nothing";
                }
                else if (args[argIndex] == DBNull.Value)
                {
                    stringArg = "DbNull.Value";
                }
                else
                {
                    try
                    {
                        stringArg = args[argIndex].ToString();
                    }
                    catch
                    {
                        stringArg = "Error converting to string";
                    }
                }

                argIndex += 1;
                stringBuilder.AppendLine(string.Format("{0}={1}", argIndex.ToString(), stringArg));
            }
        }

        return stringBuilder.ToString();
    }
}