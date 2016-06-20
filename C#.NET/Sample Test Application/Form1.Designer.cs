public partial class Form1 
{
    private System.Windows.Forms.RichTextBox txtError;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox txtUserID;
    private System.Windows.Forms.Button btnSetUserID;
    private System.Windows.Forms.Button btnLogOut;
    private System.Windows.Forms.Label lblMessage;
    private System.Windows.Forms.TextBox txtMessage;
    private System.Windows.Forms.Button button1;

    // Required by the Windows Form Designer
    private System.ComponentModel.IContainer components;

    // Form overrides dispose to clean up the component list.
    [System.Diagnostics.DebuggerNonUserCode]
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    // NOTE: The following procedure is required by the Windows Form Designer
    // It can be modified using the Windows Form Designer.  
    // Do not modify it using the code editor.
    [System.Diagnostics.DebuggerStepThrough]
    private void InitializeComponent()
    {
        this.txtError = new System.Windows.Forms.RichTextBox();
        this.label1 = new System.Windows.Forms.Label();
        this.txtUserID = new System.Windows.Forms.TextBox();
        this.btnSetUserID = new System.Windows.Forms.Button();
        this.btnLogOut = new System.Windows.Forms.Button();
        this.lblMessage = new System.Windows.Forms.Label();
        this.txtMessage = new System.Windows.Forms.TextBox();
        this.button1 = new System.Windows.Forms.Button();
        this.SuspendLayout();
         
        // txtError 
        this.txtError.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.txtError.ForeColor = System.Drawing.Color.DarkRed;
        this.txtError.Location = new System.Drawing.Point(0, 367);
        this.txtError.Name = "txtError";
        this.txtError.Size = new System.Drawing.Size(1036, 96);
        this.txtError.TabIndex = 0;
        this.txtError.Text = string.Empty;

        // Label1
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(43, 68);
        this.label1.Name = "Label1";
        this.label1.Size = new System.Drawing.Size(61, 13);
        this.label1.TabIndex = 1;
        this.label1.Text = "User name:";
      
        // txtUserID 
        this.txtUserID.Location = new System.Drawing.Point(110, 65);
        this.txtUserID.Name = "txtUserID";
        this.txtUserID.Size = new System.Drawing.Size(446, 20);
        this.txtUserID.TabIndex = 2;

        // btnSetUserID
        this.btnSetUserID.Location = new System.Drawing.Point(562, 63);
        this.btnSetUserID.Name = "btnSetUserID";
        this.btnSetUserID.Size = new System.Drawing.Size(75, 23);
        this.btnSetUserID.TabIndex = 3;
        this.btnSetUserID.Text = "Log In";
        this.btnSetUserID.UseVisualStyleBackColor = true;
        this.btnSetUserID.Click += new System.EventHandler(this.BtnSetUserID_Click_1);
       
        // btnLogOut 
        this.btnLogOut.Location = new System.Drawing.Point(657, 63);
        this.btnLogOut.Name = "btnLogOut";
        this.btnLogOut.Size = new System.Drawing.Size(75, 23);
        this.btnLogOut.TabIndex = 4;
        this.btnLogOut.Text = "Log Out";
        this.btnLogOut.UseVisualStyleBackColor = true;

        // lblMessage
        this.lblMessage.AutoSize = true;
        this.lblMessage.Location = new System.Drawing.Point(43, 121);
        this.lblMessage.Name = "lblMessage";
        this.lblMessage.Size = new System.Drawing.Size(53, 13);
        this.lblMessage.TabIndex = 5;
        this.lblMessage.Text = "Message:";

        // txtMessage
        this.txtMessage.Location = new System.Drawing.Point(110, 118);
        this.txtMessage.Multiline = true;
        this.txtMessage.Name = "txtMessage";
        this.txtMessage.Size = new System.Drawing.Size(446, 103);
        this.txtMessage.TabIndex = 6;

        // Button1
        this.button1.Location = new System.Drawing.Point(562, 116);
        this.button1.Name = "Button1";
        this.button1.Size = new System.Drawing.Size(75, 23);
        this.button1.TabIndex = 7;
        this.button1.Text = "Push to log";
        this.button1.UseVisualStyleBackColor = true;
        this.button1.Click += new System.EventHandler(this.Button1_Click_1);

        // Form1
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1036, 463);
        this.Controls.Add(this.button1);
        this.Controls.Add(this.txtMessage);
        this.Controls.Add(this.lblMessage);
        this.Controls.Add(this.btnLogOut);
        this.Controls.Add(this.btnSetUserID);
        this.Controls.Add(this.txtUserID);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.txtError);
        this.Name = "Form1";
        this.Text = "Form1";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}