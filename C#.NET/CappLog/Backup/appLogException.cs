
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;


internal class appLogException : System.Exception
{


    private const char FieldSeparator = '☺';
    private Dictionary<string, string> _Fields;

    private System.Exception _InnerException;
    protected appLogException(string Message)
    {
                _Fields = ParseFields(Message);
        _InnerException = null;
    }

    protected appLogException(string Message, System.Exception InnerException)
    {
               _Fields = ParseFields(Message);
        _InnerException = InnerException;
    }

    protected appLogException(System.Exception Exception)
    {
                _Fields = ParseFields(Exception.Message);
        _InnerException = Exception.InnerException;
    }



    public appLogException(string InClass, string InMethod, string Message, params object[] Args)
    {
        if (IsCustomExceptionMessage(Message)  == true)
        {
            _InnerException = new System.Exception(Message);
            _Fields = new Dictionary<string, string>();
            this["Message"] = "Inner exception occured!";
            this["Class"] = InClass;
            this["Method"] = InMethod;
            this["Args"] = GetArgumentsString(Args);
        }
        else
        {
            _InnerException = null;
            _Fields = new Dictionary<string, string>();
            this["Message"] = Message;
            this["Class"] = InClass;
            this["Method"] = InMethod;
            this["Args"] = GetArgumentsString(Args);
        }
    }


    public appLogException(string InClass, string InMethod, System.Exception Exception, params object[] Args)
    {
        _InnerException = Exception.InnerException;
        if (IsCustomExceptionMessage(Exception.Message) == true)
        {
            _Fields = new Dictionary<string, string>();
            this["Message"] = "Inner exception occured!";
            this["Class"] = InClass;
            this["Method"] = InMethod;
            this["Args"] = GetArgumentsString(Args);
        }
        else
        {
            _Fields = new Dictionary<string, string>();
            this["Message"] = Exception.Message;
            this["Class"] = InClass;
            this["Method"] = InMethod;
            this["Args"] = GetArgumentsString(Args);
        }
    }


    public string this[string FieldName]
    {
        get
        {
            if (FieldName == null)
                FieldName = "";
            if (FieldName.Contains("=") == true)
            {
                FieldName = FieldName.Replace("=", "");
            }
            if (_Fields.ContainsKey(FieldName) == true)
            {
                return _Fields[FieldName];
            }
            else
            {
                return "";
            }
        }
        set
        {
            if (FieldName == null)
                FieldName = "";
            if (FieldName.Contains("=") == true)
            {
                FieldName = FieldName.Replace("=", "");
            }
            if (value == null)
                value = "";
            if (_Fields.ContainsKey(FieldName) == true)
            {
                _Fields[FieldName] = value;
            }
            else
            {
                _Fields.Add(FieldName, value);
            }
        }
    }


    

    public virtual bool IsFullySpecified
    {
        get { return _Fields.ContainsKey("Message") && _Fields.ContainsKey("Class") && _Fields.ContainsKey("Method"); }
    }



    public override string Message
    {
        get { return this.ToString(); }
    }

    public System.Exception BaseException()
    {
        return new System.Exception(this["Message"], _InnerException);
    }

    public new System.Exception InnerException
    {
        get { return _InnerException; }
        set { _InnerException = value; }
    }

    public override sealed string ToString()
    {
        StringBuilder vStrBuilder = new StringBuilder();
        foreach (KeyValuePair<string, string> vKeyValuePair in _Fields)
        {
            vStrBuilder.Append(string.Format("{0}{1}={2}", FieldSeparator, vKeyValuePair.Key, vKeyValuePair.Value));

        }
        return vStrBuilder.ToString();
    }

    //public static bool IsCustomExceptionMessage
    //{
    //    get { return Message != null && Message.StartsWith(FieldSeparator) == true; }
    //}

    protected static bool IsCustomExceptionMessage(string message)
    {
        bool result = false;
        if ((message != null) & (message.StartsWith(FieldSeparator.ToString())))
        {
            result = true;
        }
        return result;
    }

    public static bool IsCustomException(Exception  message)
    {
        bool result = false;
        if (message.Message   != null && message.Message.StartsWith(FieldSeparator.ToString()) == true)
        {
            result = true;
        }
        return result;
    }


    //public static bool IsCustomException
    //{
    //    get { return Exception.Message != null && Exception.Message.StartsWith(FieldSeparator) == true; }
    //}

    public static appLogException Convert(System.Exception Exception)
    {
        return new appLogException(Exception);
    }

    protected static Dictionary<string, string> ParseFields(string Message)
    {
        Dictionary<string, string> vDicResult = new Dictionary<string, string>();
        if (Message != null && Message.StartsWith(FieldSeparator.ToString()) == true && Message.Length > 1)
        {
            Message = Message.Substring(1);
            string[] vStrElements = Message.Split(FieldSeparator);
            int vIntEqualSymbolIndex = 0;
            foreach (string vStrElement in vStrElements)
            {
                if (vStrElement.Trim().Length > 0)
                {
                    vIntEqualSymbolIndex = vStrElement.IndexOf('=');
                    if (vIntEqualSymbolIndex > 0)
                    {
                        string vStrKey = vStrElement.Substring(0, vIntEqualSymbolIndex);
                        if (vStrKey == null)
                            vStrKey = "";
                        string vStrValue = vStrElement.Substring(1 + vIntEqualSymbolIndex);
                        if (vStrValue == null)
                            vStrValue = "";
                        if (vDicResult.ContainsKey(vStrKey) == true)
                        {
                            vDicResult[vStrKey] = vStrValue;
                        }
                        else
                        {
                            vDicResult.Add(vStrKey, vStrValue);
                        }
                    }
                }
            }
            vStrElements = null;
        }
        else
        {
            vDicResult.Add("Message", Message);
        }
        if (vDicResult.ContainsKey("Message") == false)
        {
            vDicResult.Add("Message", "");
        }
        return vDicResult;
    }

    protected static string GetArgumentsString(object[] Args)
    {
        StringBuilder vStrBuilder = new StringBuilder();
        if (Args != null)
        {
            int vIntArgIndex = 0;
            while (vIntArgIndex < Args.Length)
            {
                string vStrArg = null;
                if (Args[vIntArgIndex] == null)
                {
                    vStrArg = "Nothing";
                }
                else if (Args[vIntArgIndex] == DBNull.Value )
                {
                    vStrArg = "DbNull.Value";
                }
                else
                {
                    try
                    {
                        vStrArg = Args[vIntArgIndex].ToString();
                    }
                    catch 
                    {
                        vStrArg = "Error converting to string";
                    }
                }
                vIntArgIndex += 1;
                vStrBuilder.AppendLine(string.Format("{0}={1}", vIntArgIndex.ToString(), vStrArg));
            }
        }
        return vStrBuilder.ToString();
    }
}