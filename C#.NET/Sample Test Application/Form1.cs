using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CappLog;

public partial class Form1 : Form
{
    private Log log;
    private System.Threading.Timer eventTimer;
    private string userId;
    private Dictionary<DataColumn, object> dicFields = new Dictionary<DataColumn, object>();

    public Form1()
    {
        this.InitializeComponent();
        this.Load += this.Form1_Load;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        this.dicFields.Add(new DataColumn("UserID", typeof(string)), this.userId);
        string logFileName = "c:\\users\\LogFile.Db";
        this.log = new Log(false, logFileName, null);

        this.eventTimer = new System.Threading.Timer(this.OnTimerEvent, null, 1500, System.Threading.Timeout.Infinite);
    }

    private void OnTimerEvent(object sender)
    {
        try
        {
            string logFileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\Log\\LogFile.Db";
            this.dicFields.Add(new DataColumn("UserID", typeof(string)), this.userId);
            this.log = new Log(true, logFileName, this.dicFields);
            this.log.Action(this.GetType().FullName, "OnTimerEvent", "Log called at " + string.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now));
        }
        catch
        {
        }

        this.eventTimer = new System.Threading.Timer(this.OnTimerEvent, null, 500, Timeout.Infinite);
    }

    private void BtnSetUserID_Click_1(object sender, EventArgs e)
    {
        this.userId = this.txtUserID.Text;
    }

    private void Button1_Click_1(object sender, EventArgs e)
    {
        // implement user action message here
    }
}