using System;
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

            this.comboBoxUsers.SelectedIndex = 0;

            userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            this.groupBoxTop.Text = "Name: " + userName;

            this.textBoxSubs.Text = Sock.StartServer();

            string[] split = this.textBoxSubs.Text.Split(',');
            string ipFirst = (split != null && split.Count() > 0) ? split[0] : "10.0.0";

            Sock.NewData += Sock_NewDataReceivedEventHandler;
            myIP = Sock.GetIPAddress(ipFirst);
            this.groupBoxBottom.Text = "My IP: " + myIP;
        }

        private void Chatter_Load(object sender, EventArgs e)
        {
            buttonGoConnection_Click(null, null);

            this.richTextBoxChatIn.Select();
            this.richTextBoxChatIn.SelectionStart = 0;
            this.richTextBoxChatIn.Focus();

            this._lastTyped.Interval = 4000; // 4 seconds
            this._lastTyped.Tick += _lastTyped_Tick;
            this._lastTyped.Start();
        }

        private bool isExit = false;
        private string userName;
        private string myIP;
        private Dictionary<string, string> buddyList = new Dictionary<string, string>();
        private Timer _lastTyped = new Timer();

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
                    //    int index = this.comboBoxUsers.Items.IndexOf(e.FriendName);
                    //    if (index > -1)
                    //        this.comboBoxUsers.SelectedIndex = index;
                    }
                    else
                    {
                        this.comboBoxUsers.Items.Add(e.FriendName);
                        this.buddyList[e.FriendName] = e.FriendIP;

                        // auto select combo only if first buddy found
                        if (this.comboBoxUsers.Items.Count <= 2)
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
                if (String.IsNullOrWhiteSpace(this.richTextBoxChatIn.Text.Trim()))
                {
                    richTextBoxChatIn.Clear();
                    return;
                }

                string m = this.richTextBoxChatIn.Text.TrimEnd();//.Substring(0, this.richTextBoxChatIn.Text.Length - 1);

                if (this.comboBoxUsers.Items.Count == 1)
                {
                    appendText(richTextBoxChatOut, "No friends can be found." + Environment.NewLine, Color.LightGreen);
                    // scroll it automatically
                    richTextBoxChatIn.Text = m;
                    richTextBoxChatIn.SelectionStart = richTextBoxChatIn.Text.Length;
                    richTextBoxChatIn.ScrollToCaret();
                    return;
                }

                string buddyName = this.comboBoxUsers.Text;

                // ready to send message to a buddy
                if (this.comboBoxUsers.SelectedIndex != 0 && !Sock.SendToBuddy(userName, false, buddyList[buddyName], buddyName, m, null))
                {
                    appendText(richTextBoxChatOut, "Me:\t\t", Color.LightGreen);
                    appendText(richTextBoxChatOut, m + " <remote ignored>" + Environment.NewLine, Color.LightSalmon);

                    // scroll it automatically
                    richTextBoxChatIn.Text = m;
                    richTextBoxChatIn.SelectionStart = richTextBoxChatIn.Text.Length;
                    richTextBoxChatIn.ScrollToCaret();
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

                // send message to each buddy
                if (this.comboBoxUsers.SelectedIndex == 0)
                {
                    foreach (string key in this.buddyList.Keys)
                        Sock.SendToBuddy(userName, false, buddyList[key], key, m, null);
                }
            }
        }

        private void Chatter_FormClosing(object sender, FormClosingEventArgs e)
        {
            CSavedIPs.ChangeSubList(this.textBoxSubs.Text);
            this.isExit = true;
            Sock.KillTasks();
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
            if (this.comboBoxUsers.SelectedIndex != 0)
            {
                Sock.SendToBuddy(userName, true, buddyList[this.comboBoxUsers.Text], this.comboBoxUsers.Text, "Reconnected!", null);
            }
        }

        public void InjectTestMessage(string msg)
        {
            if (!isExit)
            {
                try
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (!isExit)
                        {
                            appendText(richTextBoxChatOut, "Debug:\t\t", Color.LightGreen);
                        }
                        if (!isExit)
                        {
                            appendText(richTextBoxChatOut, msg + Environment.NewLine, Color.LightGray);
                        }
                    });
                }
                catch { }
            }
        }

        private void richTextBoxChatOut_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            // enable html tags to display in browser
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void richTextBoxChatOut_MouseUp(object sender, MouseEventArgs e)
        {   //click event
            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            MenuItem menuItem = new MenuItem("Copy");
            menuItem.Click += new EventHandler(CopyAction);
            contextMenu.MenuItems.Add(menuItem);
            contextMenu.MenuItems.Add(menuItem);

            richTextBoxChatOut.ContextMenu = contextMenu;
        }

        void CopyAction(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBoxChatOut.SelectedText);
        }

        private void richTextBoxChatIn_MouseUp(object sender, MouseEventArgs e)
        {
            //click event
            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            MenuItem menuItem = new MenuItem("Cut");
            menuItem.Click += new EventHandler(CutAction2);
            contextMenu.MenuItems.Add(menuItem);
            menuItem = new MenuItem("Copy");
            menuItem.Click += new EventHandler(CopyAction2);
            contextMenu.MenuItems.Add(menuItem);
            menuItem = new MenuItem("Paste");
            menuItem.Click += new EventHandler(PasteAction2);
            contextMenu.MenuItems.Add(menuItem);

            richTextBoxChatIn.ContextMenu = contextMenu;
        }

        void CutAction2(object sender, EventArgs e)
        {
            richTextBoxChatIn.Cut();
        }

        void CopyAction2(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBoxChatIn.SelectedText);
        }

        void PasteAction2(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                richTextBoxChatIn.Text += Clipboard.GetText(TextDataFormat.Text).ToString();
            }
        }

        private void richTextBoxChatIn_TextChanged(object sender, EventArgs e)
        {
            Sock.MyTypingStatus(true);
            _lastTyped.Stop();
            _lastTyped.Start();
        }

        void _lastTyped_Tick(object sender, EventArgs e)
        {
            Sock.MyTypingStatus(false);

            if (!isExit)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    try
                    {
                        string list = Sock.TypingBuddyList;
                        if (String.IsNullOrWhiteSpace(list))
                            this.toolStripLabelDisplay.Text = "";
                        else
                            this.toolStripLabelDisplay.Text = "Typing: " + list;
                    }
                    catch { }
                });
            }
        }
    }
}