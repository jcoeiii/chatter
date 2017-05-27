using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chatter
{
    public class CMessageHandler
    {
        public enum MsgState { Idle, ReadyForRemote, WaitingResponse, ReadyForAck, SendAck, Done };

        private MsgState _state = MsgState.Idle;
        private string _msg2Go = "";
        private int _attempt = 0;
        private Queue<string> _msgs2Send = new Queue<string>();
        private StringBuilder _buildMsg = new StringBuilder();
        private string _lastAckMsg = "";
        private string _lastAckIP = "";
        private bool _imSendingAck = false;
        private DateTime _startTime;
        private string _name;
        private string _buddyIp = "";
        private bool _msgProcessed = false;
        private string myip;

        public string Name { get { return this._name; } }

        public CMessageHandler(string name, string buddy, string myIP)
        {
            _name = name;
            _buddyIp = buddy;
            myip = myIP;
        }

        public ManualResetEvent SendDone = new ManualResetEvent(false);
        public ManualResetEvent ReceiveDone = new ManualResetEvent(false);

        public void PumpMessageFromRemote(string data, out string buddyIp, out bool isTyping)
        {
            // typing detection helpers
            buddyIp = _buddyIp;
            if (data.Contains('\t'))
                isTyping = true;
            else
                isTyping = false;

            if (!String.IsNullOrWhiteSpace(data))
            {
                // build message up
                lock (_buildMsg)
                {
                    _buildMsg.Append(data);
                }
            }
        }

        public MsgState ProcessStates(out MessageEventArgs mea)
        {
            mea = processMea();

            switch (this._state)
            {
                case MsgState.Idle:
                    if (this._msgs2Send.Count > 0)
                    {
                        lock (this._msgs2Send)
                        {
                            this._msg2Go = this._msgs2Send.Dequeue();
                        }
                        if (!String.IsNullOrWhiteSpace(this._msg2Go))
                        {
                            this._msgProcessed = false;
                            this._attempt = 0;
                            this._imSendingAck = false;
                            this._state = MsgState.ReadyForRemote;
                        }
                    }
                    break;

                case MsgState.ReadyForRemote:
                    this._startTime = DateTime.Now;

                    if (this._attempt >= 3)
                        this._state = MsgState.Idle;
                    else if (this._msgProcessed)
                        this._state = MsgState.WaitingResponse;
                    break;

                case MsgState.WaitingResponse:
                    // message was already sent out, check timeout
                    DateTime endTime = DateTime.Now;
                    if (endTime.Subtract(this._startTime).Milliseconds >= 800)
                    {
                        Sock.debug(_name + ": timeout, repeating");
                        this._msgProcessed = false;
                        this._attempt++;
                        this._state = MsgState.ReadyForRemote;
                    }
                    break;

                case MsgState.ReadyForAck:
                    this._msgProcessed = false;
                    this._msg2Go = this._lastAckMsg;
                    this._state = MsgState.SendAck;
                    break;

                case MsgState.SendAck:
                    if (this._msgProcessed)
                    {
                        this._msgProcessed = false;
                        this._imSendingAck = false;
                        this._state = MsgState.Done;
                    }
                    break;

                case MsgState.Done:
                    this._state = MsgState.Idle;
                    break;

                default: break;
            }
            return this._state;
        }

        private MessageEventArgs processMea()
        {
            string localMsg;
            int myIndex;
            lock (_buildMsg)
            {
                localMsg = _buildMsg.ToString();
                myIndex = localMsg.IndexOf("<EOF>");
                if (myIndex >= 0)
                {
                    localMsg = localMsg.Substring(0, myIndex + 1 + 4).Trim();
                    _buildMsg.Remove(0, myIndex + 1 + 4);
                }
            }

            if (myIndex >= 0)
            {
                MessageEventArgs mea = new MessageEventArgs(localMsg);

                if (mea.Valid)
                {
                    // for safety
                    _buddyIp = mea.FriendIP;

                    if (this._state == MsgState.WaitingResponse || this._state == MsgState.SendAck)
                    {

                        Sock.debug(_name + ":got ACK:" + this._state.ToString() + ":Id=" + mea.Id);
                        mea.IsAck = true;
                        if (this._state == MsgState.WaitingResponse)
                            this._state = MsgState.Done;
                        return mea;
                    }

                    if (this._imSendingAck || mea.FriendIP == myip)
                    {
                        return null;
                    }
                    else if (this._state == MsgState.Idle || this._state == MsgState.ReadyForRemote)
                    {
                        this._imSendingAck = true;
                        _lastAckMsg = generate(mea.Id, mea.FriendName, mea.FriendIP, Sock.Checksum(mea.TextFromFriend)); // no need to send entire msg payload
                        _lastAckIP = mea.FriendIP;

                        Sock.debug(_name + ":Display(sendAck)" + this._state.ToString() + ":Id=" + mea.Id);
                        this._state = MsgState.ReadyForAck;
                    }
                    else
                        return null;
                }
                else
                {
                    Sock.debug(_name + ": oops, invalid message received");
                }

                return mea;
            }
            else
            {
                return null;
            }
        }

        public string MessageReady
        {
            get
            {
                this._msgProcessed = true;
                return this._msg2Go;
            }
        }

        public void InjectTestMessage(string test)
        {
            int index = this._msg2Go.IndexOf("<EOF>");
            if (index > -1)
                this._msg2Go = this._msg2Go.Insert(index, test);
        }

        public void AddMessageToSend(string msg)
        {
            lock (this._msgs2Send)
            {
                this._msgs2Send.Enqueue(msg);
            }
        }


        // function to get unique random number
        private static int last_number = 0;
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();

        public static string GenerateMessage(string me, string ip, string msg)
        {
            int n = last_number;
            lock (syncLock)
            { // synchronize
                while (n == last_number)
                    n = getrandom.Next(1, 0xFFFF);
                last_number = n;
            }
            return generate(n.ToString("X"), me, ip, msg);
        }

        private static string generate(string id, string me, string ip, string msg)
        {
            //      |    id    | from name| from IP  | text data message       with EOF termination
            return "|" + id + "|" + me + "|" + ip + "|" + msg.Replace("|", "~") + "<EOF>";
        }
    }
}
