﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chatter
{
    public partial class Chatter : Form
    {
        public Chatter()
        {
            InitializeComponent();

            CSavedIPs.ReadFile();

            //this.textBoxSubs.Text = CSavedIPs.Subs;

            userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            this.groupBoxTop.Text = "Name: " + userName;

            this.textBoxSubs.Text = Sock.StartServer();

            string[] split = this.textBoxSubs.Text.Split(',');
            string ipFirst = (split != null && split.Count() > 0) ? split[0] : "10.0.0";

            Sock.NewData += Sock_NewDataReceivedEventHandler;
            myIP = Sock.GetIPAddress(ipFirst);
            this.groupBoxBottom.Text = "My IP: " + myIP;

            buttonGoConnection_Click(null, null);

            this.richTextBoxChatIn.Select();
            this.richTextBoxChatIn.SelectionStart = 0;
            this.richTextBoxChatIn.Focus();
        }

        private void Sock_SubChanged(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                updateSubList((string)sender);
            });
        }

        private bool isExit = false;
        private string userName;
        private string myIP;
        private List<string> buddyList = new List<string>();

        public void EnableGoButton()
        {
            if (!isExit)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.buttonGoConnection.Enabled = true;
                    appendText(richTextBoxChatOut, "Finished Search." + Environment.NewLine, Color.LightGreen);
                });
            }
        }

        private void Sock_NewDataReceivedEventHandler(MessageEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    if (this.comboBoxUsers.Items.Contains(e.FriendName))
                    {
                        int index = this.comboBoxUsers.Items.IndexOf(e.FriendName);
                        if (index > -1)
                            this.comboBoxUsers.SelectedIndex = index;
                    }
                    else
                    {
                        this.comboBoxUsers.Items.Add(e.FriendName);
                        this.buddyList.Add(e.FriendIP);

                        // auto select combo only if first buddy found
                        if (this.comboBoxUsers.Items.Count <= 1)
                        {
                            int index = this.comboBoxUsers.Items.IndexOf(e.FriendName);
                            if (index > -1)
                                this.comboBoxUsers.SelectedIndex = index;
                        }
                    }

                    appendText(richTextBoxChatOut, e.FriendName + ":\t", Color.LightGreen);
                    appendText(richTextBoxChatOut, e.TextFromFriend + Environment.NewLine, Color.LightBlue);
                    //richTextBoxChatOut.SelectionStart = richTextBoxChatOut.Text.Length;
                    // scroll it automatically
                    richTextBoxChatOut.ScrollToCaret();
                }
                catch { }
            });
        }

        private void appendText(RichTextBox box, string text, Color color)//, Font font)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            //box.SelectionFont = font;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        private void richTextBoxChat_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)Keys.Enter))
            {
                //this.richTextBoxChatIn.Text = "";

                if (this.comboBoxUsers.Text == "")
                {
                    //richTextBoxChatIn.Text = message;
                    MessageBox.Show(this, "Either no friends found or selected.");
                    return;
                }

                string m = this.richTextBoxChatIn.Text.Substring(0, this.richTextBoxChatIn.Text.Length - 1);

                // ready to send message to a friend
                if (!Sock.SendToBuddy(userName, null, buddyList[this.comboBoxUsers.SelectedIndex], m))
                {
                    appendText(richTextBoxChatOut, "Me:\t\t", Color.LightGreen);
                    appendText(richTextBoxChatOut, m + " <remote ignored>" + Environment.NewLine, Color.LightSalmon);
                    //richTextBoxChatOut.SelectionStart = richTextBoxChatOut.Text.Length;
                    // scroll it automatically
                    richTextBoxChatOut.ScrollToCaret();
                    return;
                }
                else
                {
                    appendText(richTextBoxChatOut, "Me:\t\t", Color.LightGreen);
                    appendText(richTextBoxChatOut, m + Environment.NewLine, Color.LightSalmon);
                    //richTextBoxChatOut.SelectionStart = richTextBoxChatOut.Text.Length;
                    // scroll it automatically
                    richTextBoxChatOut.ScrollToCaret();
                }

                this.richTextBoxChatIn.Text = "";
            }
        }

        private void Chatter_FormClosing(object sender, FormClosingEventArgs e)
        {
            CSavedIPs.ChangeSubList(this.textBoxSubs.Text);
            this.isExit = true;
            Sock.KillTasks();
        }

        private void updateSubList(string subs)
        {
            this.textBoxSubs.Text = subs;
            buttonGoConnection_Click(null, null);
        }

        private void buttonGoConnection_Click(object sender, EventArgs e)
        {
            CSavedIPs.ChangeSubList(this.textBoxSubs.Text);
            Sock.SetSock(this.userName, this.myIP, this.textBoxSubs.Text);
            Sock.StartSearching(this);
            appendText(richTextBoxChatOut, "Searching..." + Environment.NewLine, Color.LightGreen);
            this.buttonGoConnection.Enabled = false;
        }

        private void textBoxPort_TextChanged(object sender, EventArgs e)
        {
            this.buttonGoConnection.Enabled = true;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (this.comboBoxUsers.Items.Count > 0)
            {
                Sock.SendToBuddy(userName, null, buddyList[this.comboBoxUsers.SelectedIndex], "Reconnected!");
            }
        }
    }
}