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
using System.Threading;

namespace chatter
{
    public partial class Chatter : Form
    {
        [DllImport("user32.dll")]
        public static extern int FlashWindow(IntPtr Hwnd, bool Revert);

        #region Constructor

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
            Sock.Ack += Sock_Ack;
            myIP = Sock.GetIPAddress(ipFirst);
            this.groupBoxBottom.Text = "My IP: " + myIP;
        }

        #endregion

        #region Form Load Event

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

            this._lastTyped.Interval = 2000; // 2 seconds
            this._lastTyped.Tick += _lastTyped_Tick;
            this._lastTyped.Start();
        }

        #endregion

        #region Form Closing Event

        private void Chatter_FormClosing(object sender, FormClosingEventArgs e)
        {
            CSavedIPs.ChangeSubList(this.textBoxSubs.Text);
            this.isExit = true;
            // temp file cleanup
            try
            {
                foreach (string key in linkList.Keys)
                    if (File.Exists(linkList[key]))
                        File.Delete(linkList[key]);
            }
            catch
            { }

            Sock.KillTasks();
        }

        #endregion

        private bool isExit = false;
        private string userName;
        private string myIP;
        private Dictionary<string, string> buddyList = new Dictionary<string, string>();
        private System.Windows.Forms.Timer _lastTyped = new System.Windows.Forms.Timer();
        private Dictionary<string, string> currentTempFile = new Dictionary<string, string>();
        private Dictionary<string, string> currentChunk = new Dictionary<string, string>();
        private Dictionary<string, int> currentChunkId = new Dictionary<string, int>();
        private Dictionary<string, string> linkList = new Dictionary<string, string>();

        #region Sock Events

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
                        if (e.ChunkId == -1)
                        {
                            if (currentChunkId[e.FriendIP] != -1)
                            {
                                byte[] data = StringCompressor.ToHexBytes(currentChunk[e.FriendIP]);

                                if (!File.Exists(currentTempFile[e.FriendIP]))
                                    File.WriteAllBytes(currentTempFile[e.FriendIP], data);
                                else
                                    appendAllBytes(currentTempFile[e.FriendIP], data);
                            }

                            appendText(richTextBoxChatOut, e.FriendName + ":\tSent a file->File_Waiting: ", Color.LightGreen);
                            richTextBoxChatOut.InsertLink(e.FileName + "   ");
                            if (linkList.ContainsKey(e.FileName + "   "))
                                linkList[e.FileName + "   "] = currentTempFile[e.FriendIP];
                            else
                                linkList.Add(e.FileName + "   ", currentTempFile[e.FriendIP]);
                            richTextBoxChatOut.AppendText("      " + Environment.NewLine);
                            richTextBoxChatOut.ScrollToCaret();
                        }
                        else
                        {
                            if (e.ChunkId == 0)
                            {
                                currentTempFile[e.FriendIP] = Path.GetTempFileName();
                                currentChunk[e.FriendIP] = e.FileData;
                                currentChunkId[e.FriendIP] = 0;
                            }
                            else
                            {
                                // if this is a different chunk than before write it out
                                if (currentChunkId[e.FriendIP] != e.ChunkId)
                                {
                                    byte[] data = StringCompressor.ToHexBytes(currentChunk[e.FriendIP]);

                                    if (!File.Exists(currentTempFile[e.FriendIP]))
                                        File.WriteAllBytes(currentTempFile[e.FriendIP], data);
                                    else
                                        appendAllBytes(currentTempFile[e.FriendIP], data);
                                }
                                currentChunk[e.FriendIP] = e.FileData;
                                currentChunkId[e.FriendIP] = e.ChunkId;
                            }
                        }
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

        #endregion

        #region General Helpers

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

        static private void appendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
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

        #endregion

        #region Form Control Events

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

        private void richTextBoxChatOut_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (String.IsNullOrEmpty(e.LinkText))
            {
            }
            else if (linkList.ContainsKey(e.LinkText))
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.FileName = e.LinkText.TrimEnd();
                saveFileDialog1.Title = "Save File";

                // If the file name is not an empty string open it for saving.  
                if (saveFileDialog1.ShowDialog() == DialogResult.OK && saveFileDialog1.FileName != "")
                {
                    try
                    {
                        File.Copy(linkList[e.LinkText], saveFileDialog1.FileName);
                    }
                    catch
                    { }
                }
            }
            else
            {
                // enable html tags to display in browser
                System.Diagnostics.Process.Start(e.LinkText);
            }
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
            GC.Collect(); // just helps ok
        }

        private void _lastTyped_Tick(object sender, EventArgs e)
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

                if (buddyList.ContainsKey(this.comboBoxUsers.Text))
                {
                    string file = files[0];
                    appendText(richTextBoxChatOut, "Me:\t\t", Color.LightGreen);
                    appendText(richTextBoxChatOut, "Sending File: " + file + Environment.NewLine, Color.LightSalmon);
                    richTextBoxChatOut.ScrollToCaret();

                    string ip = buddyList[this.comboBoxUsers.Text];
                    string fullname = this.comboBoxUsers.Text;

                    Task.Factory.StartNew(() => dotheFileXfer(file, ip, fullname));
                }
            }
        }

        #endregion

        #region File Drag & Drop Stream Helpers

        private void dotheFileXfer(string filepath, string ip, string fullname)
        {
            try
            {
                using (Stream source = File.OpenRead(filepath))
                {
                    byte[] buffer = new byte[1024 * 7];
                    int bytesRead;
                    int chunkCount = 0;

                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (isExit)
                            return;

                        if (!handleFileIO(buffer, bytesRead, filepath, chunkCount++, ip, fullname))
                            throw new Exception("file transfer errors on chunk: " + chunkCount);

                        if (chunkCount % 12 == 0)
                            InjectTestMessage("Working...");
                    }

                    // send end of file confirmation
                    buffer = new byte[0];
                    if (!handleFileIO(buffer, 0, filepath, -1, ip, fullname))
                        if (!handleFileIO(buffer, 0, filepath, -1, ip, fullname))
                            throw new Exception("file transfer errors on chunk: " + chunkCount);

                    InjectTestMessage("Completed.");
                }
            }
            catch (Exception ex)
            {
                InjectTestMessage("ERROR: " + filepath + ":" + ex.ToString());
            }
        }

        private MessageEventArgs _e = null;
        private ManualResetEvent AckDone = new ManualResetEvent(false);
        private void Sock_Ack(MessageEventArgs e)
        {
            _e = e;
            AckDone.Set();
        }

        private bool handleFileIO(byte[] buffer, int length, string filepath, int chunk, string ip, string fullname)
        {
            string fileData = StringCompressor.ToHexString(buffer, length);
            string m = "<OBJECT>" + "FILE*" + chunk.ToString() + "*" + Path.GetFileName(filepath).Replace("?", "_") + "*" + fileData;
            string chk = Sock.Checksum(Sock.Checksum(m)); // do it twice, trust me

            _e = null;
            AckDone.Reset();
            Sock.SendToBuddy(userName, false, ip, fullname, m, null);

            // wait for confirmations
            AckDone.WaitOne(5000);

            if (!isExit && (_e == null || !_e.Valid || _e.Checksum != chk))
            {
                // try to repeat once
                _e = null;
                AckDone.Reset();
                Sock.SendToBuddy(userName, false, ip, fullname, m, null);
                // wait for confirmations
                AckDone.WaitOne(5000);

                if (_e == null || !_e.Valid || _e.Checksum != chk)
                    return false;
            }

            return true;
        }

        #endregion

        #region Debug Helper

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

        #endregion
    }
}