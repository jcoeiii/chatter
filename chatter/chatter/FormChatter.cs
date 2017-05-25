using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.IO;

namespace chatter
{
    public partial class Chatter : Form
    {
        [DllImport("user32.dll")]
        public static extern int FlashWindow(IntPtr Hwnd, bool Revert);

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
            if (myIP == "?")
            {
                appendText(richTextBoxChatOut, "Warning: IP not determined." + Environment.NewLine, Color.LightGreen);
            }
            else
            {
                buttonGoConnection_Click(null, null);
            }
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

                    if (e.IsFile)
                    {
                        appendText(richTextBoxChatOut, e.FriendName + ":\t", Color.LightGreen);

                        LinkLabel link = new LinkLabel();
                        link.Text = e.FileName;
                        link.Name = e.FileName;
                        
                        link.LinkClicked += new LinkLabelLinkClickedEventHandler(this.link_LinkClicked);

                        LinkLabel.Link data = new LinkLabel.Link();
                        data.LinkData = e.FileData;
                        link.Links.Add(data);
                        link.AutoSize = true;
                        
                        appendText(richTextBoxChatOut, "File waiting: ", Color.Red);

                        link.Location = this.richTextBoxChatOut.GetPositionFromCharIndex(this.richTextBoxChatOut.TextLength);
                        richTextBoxChatOut.Controls.Add(link);
                        richTextBoxChatOut.AppendText(e.FileName + "   " + Environment.NewLine);

                        //richTextBoxChatOut.SelectionStart = this.richTextBoxChatOut.TextLength;

                        richTextBoxChatOut.ScrollToCaret();
                    }
                    else
                    {
                        appendText(richTextBoxChatOut, e.FriendName + ":\t", Color.LightGreen);
                        appendText(richTextBoxChatOut, e.TextFromFriend + Environment.NewLine, Color.LightBlue);
                        // scroll it automatically
                        richTextBoxChatOut.ScrollToCaret();
                    }

                    // make the form blink on taskbar if not already active
                    FlashWindow(this.Handle, false);
                }
                catch { }
            });
        }

        private void link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //System.Diagnostics.Process.Start(e.Link.Text.ToString());

            // Displays a SaveFileDialog so the user can save the Image  
            // assigned to Button2.  
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.FileName = ((LinkLabel)sender).Name;
            //saveFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            saveFileDialog1.Title = "Save File";

            // If the file name is not an empty string open it for saving.  
            if (saveFileDialog1.ShowDialog() == DialogResult.OK && saveFileDialog1.FileName != "")
            {
                File.WriteAllBytes(saveFileDialog1.FileName, (byte[])e.Link.LinkData);
            }

            richTextBoxChatOut.Controls.Remove((Control)sender);
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
                if (Control.ModifierKeys != Keys.Shift)
                {
                    Sock.MyTypingStatus(false);

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

                    // handle multi lines nicely
                    if (m.Contains('\n'))
                    {
                        string[] splits = m.Split(new char[] { '\n' }, StringSplitOptions.None);
                        m = "";
                        foreach (string s in splits)
                            m += s + "\n\t\t";
                        m = m.TrimEnd();
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
            updateToolStrip();
            _lastTyped.Stop(); // resets timer
            _lastTyped.Start();
        }

        void _lastTyped_Tick(object sender, EventArgs e)
        {
            Sock.MyTypingStatus(false);
            updateToolStrip();
        }

        private void updateToolStrip()
        {
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

        private void Chatter_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Chatter_DragDrop(object sender, DragEventArgs e)
        {
            if (this.comboBoxUsers.SelectedIndex == 0)
            {
                appendText(richTextBoxChatOut, "Warning: Cannot send to all users." + Environment.NewLine, Color.LightGreen);
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string filepath in files)
                {
                    try
                    {
                        if (buddyList.ContainsKey(this.comboBoxUsers.Text))
                        {
                            FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

                            // make message for file object
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                byte[] bytes = ms.ToArray();
                                string fileData = StringCompressor.ToHexString(bytes);
                                 
                                if (fileData.Length < 1024 * 10000)
                                { 
                                    string m = "<OBJECT>" + "FILE\t" + Path.GetFileName(filepath) + "\t" + fileData;

                                    Sock.SendToBuddy(userName, false, buddyList[this.comboBoxUsers.Text], this.comboBoxUsers.Text, m, null);
                                    appendText(richTextBoxChatOut, "Me:\t\t", Color.LightGreen);
                                    appendText(richTextBoxChatOut, "File sent : " + filepath + Environment.NewLine, Color.LightSalmon);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        appendText(richTextBoxChatOut, "ERROR: " + filepath  + ":" + ex.ToString() + Environment.NewLine, Color.LightGreen);
                    }
                    finally
                    {
                        // scroll it automatically
                        richTextBoxChatOut.ScrollToCaret();
                    }
                }
            }
        }
    }
}