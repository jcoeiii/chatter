namespace chatter
{
    partial class Chatter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxTop = new System.Windows.Forms.GroupBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonGoConnection = new System.Windows.Forms.Button();
            this.comboBoxUsers = new System.Windows.Forms.ComboBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.groupBoxBottom = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.richTextBoxChatOut = new System.Windows.Forms.RichTextBox();
            this.richTextBoxChatIn = new System.Windows.Forms.RichTextBox();
            this.groupBoxTop.SuspendLayout();
            this.groupBoxBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxTop
            // 
            this.groupBoxTop.Controls.Add(this.buttonConnect);
            this.groupBoxTop.Controls.Add(this.buttonGoConnection);
            this.groupBoxTop.Controls.Add(this.comboBoxUsers);
            this.groupBoxTop.Controls.Add(this.textBoxPort);
            this.groupBoxTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxTop.Location = new System.Drawing.Point(0, 0);
            this.groupBoxTop.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxTop.Name = "groupBoxTop";
            this.groupBoxTop.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxTop.Size = new System.Drawing.Size(396, 50);
            this.groupBoxTop.TabIndex = 0;
            this.groupBoxTop.TabStop = false;
            this.groupBoxTop.Text = "Name:";
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(252, 20);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(59, 19);
            this.buttonConnect.TabIndex = 3;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonGoConnection
            // 
            this.buttonGoConnection.Location = new System.Drawing.Point(55, 20);
            this.buttonGoConnection.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonGoConnection.Name = "buttonGoConnection";
            this.buttonGoConnection.Size = new System.Drawing.Size(45, 19);
            this.buttonGoConnection.TabIndex = 2;
            this.buttonGoConnection.Text = "Look";
            this.buttonGoConnection.UseVisualStyleBackColor = true;
            this.buttonGoConnection.Click += new System.EventHandler(this.buttonGoConnection_Click);
            // 
            // comboBoxUsers
            // 
            this.comboBoxUsers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxUsers.FormattingEnabled = true;
            this.comboBoxUsers.Location = new System.Drawing.Point(104, 20);
            this.comboBoxUsers.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBoxUsers.Name = "comboBoxUsers";
            this.comboBoxUsers.Size = new System.Drawing.Size(144, 21);
            this.comboBoxUsers.TabIndex = 1;
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(9, 21);
            this.textBoxPort.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(42, 20);
            this.textBoxPort.TabIndex = 0;
            this.textBoxPort.Text = "10.0";
            this.textBoxPort.TextChanged += new System.EventHandler(this.textBoxPort_TextChanged);
            // 
            // groupBoxBottom
            // 
            this.groupBoxBottom.Controls.Add(this.splitContainer1);
            this.groupBoxBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxBottom.Location = new System.Drawing.Point(0, 50);
            this.groupBoxBottom.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxBottom.Name = "groupBoxBottom";
            this.groupBoxBottom.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxBottom.Size = new System.Drawing.Size(396, 228);
            this.groupBoxBottom.TabIndex = 1;
            this.groupBoxBottom.TabStop = false;
            this.groupBoxBottom.Text = "My IP:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(2, 15);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxChatOut);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxChatIn);
            this.splitContainer1.Size = new System.Drawing.Size(392, 211);
            this.splitContainer1.SplitterDistance = 134;
            this.splitContainer1.TabIndex = 1;
            // 
            // richTextBoxChatOut
            // 
            this.richTextBoxChatOut.BackColor = System.Drawing.SystemColors.InfoText;
            this.richTextBoxChatOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxChatOut.ForeColor = System.Drawing.Color.LightGreen;
            this.richTextBoxChatOut.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxChatOut.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.richTextBoxChatOut.Name = "richTextBoxChatOut";
            this.richTextBoxChatOut.ReadOnly = true;
            this.richTextBoxChatOut.Size = new System.Drawing.Size(392, 134);
            this.richTextBoxChatOut.TabIndex = 0;
            this.richTextBoxChatOut.Text = "";
            // 
            // richTextBoxChatIn
            // 
            this.richTextBoxChatIn.BackColor = System.Drawing.SystemColors.HighlightText;
            this.richTextBoxChatIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxChatIn.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.richTextBoxChatIn.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxChatIn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.richTextBoxChatIn.Name = "richTextBoxChatIn";
            this.richTextBoxChatIn.Size = new System.Drawing.Size(392, 73);
            this.richTextBoxChatIn.TabIndex = 0;
            this.richTextBoxChatIn.Text = "";
            this.richTextBoxChatIn.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.richTextBoxChat_KeyPress);
            // 
            // Chatter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(396, 278);
            this.Controls.Add(this.groupBoxBottom);
            this.Controls.Add(this.groupBoxTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Chatter";
            this.Text = "Chatter V1.4.5";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Chatter_FormClosing);
            this.groupBoxTop.ResumeLayout(false);
            this.groupBoxTop.PerformLayout();
            this.groupBoxBottom.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxTop;
        private System.Windows.Forms.ComboBox comboBoxUsers;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.GroupBox groupBoxBottom;
        private System.Windows.Forms.RichTextBox richTextBoxChatOut;
        private System.Windows.Forms.Button buttonGoConnection;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox richTextBoxChatIn;
        private System.Windows.Forms.Button buttonConnect;
    }
}

