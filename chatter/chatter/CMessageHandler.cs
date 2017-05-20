using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatter
{
    public class CMessageHandler
    {
        public enum MsgState { Idle, ReadyForRemote, WaitingResponse, AppendAck };

        private MsgState _state = MsgState.Idle;
        private bool _waitingForResponse = false;
        private bool _lastWasAck = false;
        private string _msg2Go = "";
        private List<string> _msgs2Send = new List<string>();
        private StringBuilder _buildMsg = new StringBuilder();
        private string _lastRecvedMsg = "";
        private string currentId = "";
        private DateTime _startTime;

        //public CMessageHandler()
        //{
        //}

        public MessageEventArgs PumpMessageFromRemote(byte[] bytes, int bytesRec)
        {
            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

            if (!String.IsNullOrEmpty(data))
            {
                // build local message up
                _buildMsg.Append(data);

                string localMsg = _buildMsg.ToString().Trim();

                if (localMsg.IndexOf("<EOF>") > -1)
                {
                    MessageEventArgs mea = new MessageEventArgs(localMsg);
                    //string tmp = localMsg;
                    _buildMsg.Clear();

                    if (mea.Valid)
                    {
                        //if (this._state == MsgState.WaitingResponse)
                        //{
                        // this was a reply ACK
                        if (currentId == mea.Id)
                        {
                            currentId = "";
                            this._state = MsgState.Idle;
                            return null; // don't send a repeat back to the user
                        }
                        //}

                        _lastRecvedMsg = localMsg;

                        if (this._state == MsgState.Idle)
                            this._state = MsgState.AppendAck;

                        currentId = mea.Id;
                        return mea;
                    }
                    //else
                    // oops, invalid message received
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
                        //this._msg2Go = this._msgs2Send[0];
                        lock (this._msgs2Send)
                        {
                            this._msg2Go = this._msgs2Send[0];
                            this._msgs2Send.RemoveAt(0);
                        }
                        MessageEventArgs mea = new MessageEventArgs(this._msg2Go);
                        this.currentId = mea.Id;

                        this._state = MsgState.ReadyForRemote;
                    }
                    break;

                case MsgState.ReadyForRemote:
                    if (this._waitingForResponse)
                    {
                        if (this._lastWasAck)
                        {
                            this._lastWasAck = false;
                            this._state = MsgState.Idle;
                        }
                        else
                            this._state = MsgState.WaitingResponse;
                    }
                    break;

                case MsgState.WaitingResponse:
                    // message was already sent out, check timeout
                    DateTime endTime = DateTime.Now;
                    TimeSpan total = endTime.Subtract(this._startTime);
                    if (total.Milliseconds >= 400) // ~5 sec
                    {
                        this._waitingForResponse = false;
                        this._state = MsgState.ReadyForRemote;
                    }
                    break;

                case MsgState.AppendAck:
                    this._lastWasAck = true;
                    AddMessageToSend(_lastRecvedMsg);
                    this._state = MsgState.Idle;
                    break;

                default: break;
            }

            return this._state;
        }

        public byte[] MessageReady
        {
            get
            {
                this._waitingForResponse = true;
                this._startTime = DateTime.Now;
                return System.Text.Encoding.ASCII.GetBytes(this._msg2Go);
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
                this._msgs2Send.Add(msg);
            }
        }


        //Function to get random number
        private static int last_number = 0;
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();

        public static string GenerateMessage(string me, string ip, string msg)
        {
            lock (syncLock)
            { // synchronize

                int n = last_number;
                while (n == last_number)
                    n = getrandom.Next(1, 0xFFFF);
                last_number = n;
            }
            //                          id      | from name| from IP  | text data message
            return last_number.ToString("X") + "|" + me + "|" + ip + "|" + msg.Replace("|", "~") + "<EOF>";
        }
    }
}
