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
        public enum MsgState { Idle, ReadyForRemote, WaitingResponse, AppendAck };

        private MsgState _state = MsgState.Idle;
        private bool _waitingForResponse = false;
        private string _msg2Go = "";
        private int _attempt = 0;
        private Queue<string> _msgs2Send = new Queue<string>();
        private StringBuilder _buildMsg = new StringBuilder();
        private string _lastAckMsg = "";
        private string _lastAckIP = "";
        private bool _ackWasSent = false;
        private string currentId = "";
        private DateTime _startTime;
        private string _name;
        private bool _completed = false;
        private string _buddyIp = "";

        public CMessageHandler(string name, string buddy)
        {
            _name = name;
            _buddyIp = buddy;
        }

        public ManualResetEvent SendDone = new ManualResetEvent(false);
        public ManualResetEvent ReceiveDone = new ManualResetEvent(false);

        public MessageEventArgs PumpMessageFromRemote(string data, out string buddyIp, out bool isTyping)
        {
            // typing detection helpers
            buddyIp = _buddyIp;
            if (data.Contains('\t'))
                isTyping = true;
            else
                isTyping = false;

            if (!String.IsNullOrEmpty(data))
            {
                // build local message up
                _buildMsg.Append(data);

                string localMsg = _buildMsg.ToString();
                int myIndex = localMsg.IndexOf("<EOF>");
                if (myIndex > -1)
                {
                    localMsg = localMsg.Substring(0, myIndex + 1 + 4).Trim();
                    _buildMsg.Remove(0, myIndex + 1 + 4);

                    MessageEventArgs mea = new MessageEventArgs(localMsg);
                    if (mea.Valid)
                    {
                        // for safety
                        _buddyIp = mea.FriendIP;

                        // this was a reply ACK?
                        if (currentId == mea.Id)
                        {
                            Sock.debug(_name + ":got ACK:" + this._state.ToString() + ":Id=" + mea.Id); 
                            this._completed = true;
                            if (this._waitingForResponse)
                            {
                                this._waitingForResponse = false;
                                this._state = MsgState.Idle;
                            }
                            return null; // don't send a repeat back to the user screen
                        }
                        else
                        {
                            _lastAckMsg = generate(mea.Id, mea.FriendName, mea.FriendIP, ""); // no need to send entire msg payload
                            _lastAckIP = mea.FriendIP;

                            if (!this._waitingForResponse)
                            {
                                Sock.debug(_name + ":AppendAck:Current=" + currentId + ":Id=" + mea.Id);
                                this._state = MsgState.AppendAck;
                            }
                            else
                            {
                                Sock.debug(_name + ":Msg:" + this._state.ToString() + "Current=" + currentId + ":Id=" + mea.Id);
                            }
                        }
                        return mea;
                    }
                    else
                        Sock.debug(_name + ": oops, invalid message received");
                }
                else if (_buildMsg.Length > 250 && localMsg == "")
                    _buildMsg.Clear();
            }

            return null;
        }


        public MsgState ProcessStates()
        {
            switch (this._state)
            {
                case MsgState.Idle:
                    if (this._msgs2Send.Count > 0)
                    {
                        this._msg2Go = this._msgs2Send.Dequeue();
                        if (!String.IsNullOrWhiteSpace(this._msg2Go))
                        {
                            this._completed = false;
                            this._attempt = 0;
                            this._ackWasSent = false;
                            this._waitingForResponse = false;
                            this._state = MsgState.ReadyForRemote;
                        }
                    }
                    break;

                case MsgState.ReadyForRemote:
                    if (this._attempt >= 3 || this._completed)
                    {
                        this._state = MsgState.Idle;
                    }
                    else if (this._ackWasSent)
                    {
                        this._ackWasSent = false;
                        this._state = MsgState.Idle;
                    }
                    else if (!this._waitingForResponse)
                    {
                        this._waitingForResponse = true;
                        this._state = MsgState.WaitingResponse;
                    }
                    this._attempt++;
                    break;

                case MsgState.WaitingResponse:
                    // message was already sent out, check timeout
                    DateTime endTime = DateTime.Now;
                    if (endTime.Subtract(this._startTime).Milliseconds >= 700)
                    {
                        Sock.debug(_name + ": timeout, repeating");
                        this._waitingForResponse = false;
                        this._state = MsgState.ReadyForRemote;
                    }
                    break;

                case MsgState.AppendAck:
                    // force one shot ACK
                    this._attempt = 0;
                    this._ackWasSent = true;
                    this.currentId = this._lastAckIP;
                    this._msg2Go = this._lastAckMsg;
                    this._state = MsgState.ReadyForRemote;
                    break;

                default: break;
            }

            return this._state;
        }

        public string MessageReady
        {
            get
            {
                MessageEventArgs mea = new MessageEventArgs(this._msg2Go);
                currentId = mea.Id;
                this._startTime = DateTime.Now; // stamp it right now, going to send out over socket
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
            this._msgs2Send.Enqueue(msg);
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
            //     id    | from name| from IP  | text data message       with EOF termination
            return id + "|" + me + "|" + ip + "|" + msg.Replace("|", "~") + "<EOF>";
        }
    }
}
