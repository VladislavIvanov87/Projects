using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;



public partial class Form1 : Form
{


    private Log _Log;
    private System.Threading.Timer _EventTimer;
    private string _UserId;
    private Dictionary<DataColumn, object> _DicFields = new Dictionary<DataColumn, object>();

    private void Form1_Load(System.Object sender, System.EventArgs e)
    {
        _DicFields.Add(new DataColumn("UserID", typeof(string)), _UserId);
               string LogFileName = "c:\\users\\LogFile.Db";
               _Log = new Log(false, LogFileName, null );

        _EventTimer = new System.Threading.Timer(this.OnTimerEvent, null, 1500, System.Threading.Timeout.Infinite );
        
    }


    private void OnTimerEvent(object Sender)
    {
        try
        {
            string LogFileName = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\Log\\LogFile.Db";
            _DicFields.Add(new DataColumn("UserID", typeof(string)), _UserId);
            _Log = new Log(true, LogFileName, _DicFields);
            _Log.Action(this.GetType().FullName, "OnTimerEvent", "Log called at " + String.Format("{0:dd/MM/yyyy HH:mm:ss}",System.DateTime.Now));

        }
        catch 
        {
        }
        _EventTimer = new System.Threading.Timer(this.OnTimerEvent, null, 500, Timeout.Infinite);
    }

    private void btnSetUserID_Click_1(object sender, EventArgs e)
    {
        _UserId = txtUserID.Text;
    }
    private void Button1_Click_1(object sender, EventArgs e)
    {
      //implement user action message here
    }

    public Form1()
    {
        InitializeComponent();
        Load += Form1_Load;
    }



}

