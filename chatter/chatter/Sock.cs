//#define DEBUG_LOOPBACK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.IO;

namespace chatter
{
    public class Sock
    {
        // helped a lot
        // https://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx

        static private int Port = 25011;

        static private int threadRunningCount = 0;
        static private bool stayAlive = true;
        static private string subsList;
        static private string myIP;
        static private string myPCName;
        static private Dictionary<string, Queue<string>> ipBuddyMessages = new Dictionary<string, Queue<string>>();
        static private Dictionary<string, string> ipBuddyFullNameLookup = new Dictionary<string, string>();
        static private Dictionary<string, bool> ipBuddyIsTyping = new Dictionary<string, bool>();
        static private bool myTypingStatus = false;
        static private List<string> ipsInUse = new List<string>();

        static public event NewDataReceivedEventHandler NewData;
        public delegate void NewDataReceivedEventHandler(MessageEventArgs e);

        static protected void OnNewDataReceived(MessageEventArgs e)
        {
            NewDataReceivedEventHandler handler = NewData;

#if !DEBUG_LOOPBACK
            if (e.FriendIP == myIP)
            {
                debug("Newdata event ignored my ip!");
                return;
            }
#endif
            lock (handler)
            {
                if (handler != null)
                {
                    handler(e);
                }
            }
        }

        static public void MyTypingStatus(bool status)
        {
            myTypingStatus = status;
        }

        static public string TypingBuddyList
        {
            get
            {
                string typers = "";
                try
                {
                    foreach (string ip in ipsInUse)
                    {
                        if (ipBuddyFullNameLookup.ContainsKey(ip) && ipBuddyIsTyping[ip])
                            typers += ipBuddyFullNameLookup[ip] + " ";
                    }
                }
                catch { }
                return typers;
            }
        }

        static public bool SetSock(string myName, string ip, string subsList)
        {
            try
            {
                KillTasks();
                stayAlive = true;
                myPCName = myName;
                Sock.subsList = subsList;
                myIP = ip;
                return true;
            }
            catch { return false; }
        }

        static public void KillTasks()
        {
            stayAlive = false;
            while (threadRunningCount > 1)
                Thread.Sleep(200);
        }

        static void beginThread()
        {
            threadRunningCount++;
            debug("Thread count = " + threadRunningCount);
        }

        static void endThread()
        {
            threadRunningCount--;
            debug("Thread count = " + threadRunningCount);
        }

        static private void StartListening(IPHostEntry ipHostInfo)
        {
            //beginThread();

            debug("Listening agent launched");
            while (stayAlive)
            {
                try
                {
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

                    // Create a TCP/IP socket.  
                    Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    while (stayAlive)
                    {
                        Socket handler = listener.Accept();

                        string buddyIp = handler.RemoteEndPoint.ToString();
                        int index = buddyIp.IndexOf(':');
                        if (index > -1)
                            buddyIp = buddyIp.Substring(0, index);

#if !DEBUG_LOOPBACK
                        if (buddyIp == myIP)
                        { // just ingore this situation
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Disconnect(false);
                            handler.Close();
                            debug("Listening agent ignored my ip!");
                            continue;
                        }
#endif
                        if (!checkForRunningThread(buddyIp))
                        {
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Disconnect(false);
                            handler.Close();
                            debug("Listening agent skipped ip: " + buddyIp);
                            continue;
                        }

                        debug("SSListener started for " + buddyIp);
                        Task.Factory.StartNew(() => StartServerSide(handler, buddyIp));
                    }
                }
                catch (Exception e)
                {
                    debug("Listening agent exception: " + e.ToString());
                }

                Thread.Sleep(200);
            }

            //endThread();
        }

        static private void StartServerSide(Socket handler, string buddyIp)
        {
            beginThread();

            int waitingCount = 0;

            debug("Server thread launched");
            try
            {
                CMessageHandler m = new CMessageHandler("ServerMH", buddyIp);
                int timeout = 0;

                if (!ipBuddyMessages.ContainsKey(buddyIp))
                    ipBuddyMessages.Add(buddyIp, new Queue<string>());

                if (!ipBuddyIsTyping.ContainsKey(buddyIp))
                    ipBuddyIsTyping.Add(buddyIp, false);

                handler.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);

                // An incoming connection needs to be processed.  
                while (stayAlive)
                {
                    if (timeout >= 30)
                    {
                        debug("Server timedout for " + buddyIp);
                        break;
                    }

                    if (waitingCount <= 0 && ipBuddyMessages[buddyIp].Count > 0)
                    {
                        lock (ipBuddyMessages[buddyIp])
                        {
                            m.AddMessageToSend(ipBuddyMessages[buddyIp].Dequeue());
                        }
                    }

                    CMessageHandler.MsgState state = m.ProcessStates();

                    // Write to Client as needed
                    if (state == CMessageHandler.MsgState.ReadyForRemote)
                    {
#if (DEBUG)
                        m.InjectTestMessage(";S" + timeout);
#endif
                        debug("Server sent out msg to " + buddyIp);

                        send(handler, m.MessageReady, m);
                    }
                    else
                    {
                        if (myTypingStatus)
                            send(handler, "\t", m);
                        else
                            send(handler, " ", m);
                    }
                    m.SendDone.WaitOne(500);

                    if (!receive(handler, m))
                        timeout++;
                    else
                        m.ReceiveDone.WaitOne(500);

                    if (waitingCount > 0)
                        waitingCount--;

                    Thread.Sleep(200);
                    CSavedIPs.AppendIP(buddyIp);
                }
            }
            catch (Exception e)
            {
                debug("Server exception for " + buddyIp + e.ToString());
                if (_debug != null)
                    _debug.InjectTestMessage("User aborted: " + ipBuddyFullNameLookup[buddyIp]);
            }

            if (handler != null)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Disconnect(false);
                handler.Close();
                //handler.Dispose();
            }


            lock (ipsInUse)
            {
                if (ipsInUse.Contains(buddyIp))
                    ipsInUse.Remove(buddyIp);
            }
            //lock (ipBuddyMessages)
            //{
            //    if (ipBuddyMessages.ContainsKey(buddyIp))
            //        ipBuddyMessages.Remove(buddyIp);
            //}

            endThread();
        }

        static public string StartServer()
        {
            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the   
            // host running the application.
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.Resolve(hostName); // obsolete but it works

            if (CSavedIPs.Subs == "")
            {
                string subs = "";
                foreach (IPAddress i in ipHostInfo.AddressList)
                {
                    if (subs != "")
                        subs += ",";
                    string ip = i.ToString();
                    int loc = ip.IndexOf('.');
                    loc = ip.IndexOf('.', loc + 1);
                    loc = ip.IndexOf('.', loc + 1);
                    subs += ip.Substring(0, loc);
                }
                CSavedIPs.ChangeSubList(subs);
            }

            Task.Factory.StartNew(() => StartListening(ipHostInfo));

            return CSavedIPs.Subs;
        }

        static private bool checkForRunningThread(string buddyIp)
        {
            bool justAdded = false;
            lock (ipsInUse)
            {
                if (ipsInUse.Contains(buddyIp))
                {
                    return false;
                }
                else
                {
                    ipsInUse.Add(buddyIp);
                    justAdded = true;
                }
            }
            return justAdded;
        }

        static public void StartClientSide(Socket sender, string buddyIp)
        {
            beginThread();

            int timeout = 0;
            debug("Client thread launched");

            try
            {
#if !DEBUG_LOOPBACK
                if (!checkForRunningThread(buddyIp))
                {
                    endThread();
                    debug("Client ignored this ip: " + buddyIp);
                    return;
                }
#endif
                

                if (!ipBuddyMessages.ContainsKey(buddyIp))
                    ipBuddyMessages.Add(buddyIp, new Queue<string>());

                if (!ipBuddyIsTyping.ContainsKey(buddyIp))
                    ipBuddyIsTyping.Add(buddyIp, false);

                CMessageHandler m = new CMessageHandler("ClientMH", buddyIp);

                // Enter the working loop
                while (stayAlive)
                {
                    if (timeout >= 12)
                    {
                        debug("Client timed out for " + buddyIp);
                        break;
                    }

                    if (ipBuddyMessages[buddyIp].Count > 0)
                    {
                        lock (ipBuddyMessages[buddyIp])
                        {
                            m.AddMessageToSend(ipBuddyMessages[buddyIp].Dequeue());
                        }
                    }

                    CMessageHandler.MsgState state = m.ProcessStates();

                    // Write to Server as needed
                    if (state == CMessageHandler.MsgState.ReadyForRemote)
                    {
#if (DEBUG)
                        m.InjectTestMessage(":C" + timeout);
#endif
                        debug("Client sent out msg to " + buddyIp);

                        send(sender, m.MessageReady, m);
                    }
                    else
                    {
                        if (myTypingStatus)
                            send(sender, "\t", m);
                        else
                            send(sender, " ", m);
                    }
                    m.SendDone.WaitOne(500);


                    if (!receive(sender, m))
                        timeout++;
                    else
                        m.ReceiveDone.WaitOne(500);

                    Thread.Sleep(200);
                    CSavedIPs.AppendIP(buddyIp);
                }

                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            catch (Exception e)
            {
                debug("Client exception for " + buddyIp + e.ToString());
                if (_debug != null)
                    _debug.InjectTestMessage("User aborted: " + ipBuddyFullNameLookup[buddyIp]);
            }

            lock (ipsInUse)
            {
                if (ipsInUse.Contains(buddyIp))
                    ipsInUse.Remove(buddyIp);
            }
            //lock (ipBuddyMessages)
            //{
            //    if (ipBuddyMessages.ContainsKey(buddyIp))
            //        ipBuddyMessages.Remove(buddyIp);
            //}

            endThread();
        }

        static private bool receive(Socket client, CMessageHandler m)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject(m);
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                return true;
            }
            catch (Exception e)
            {
                debug(e.ToString());
                return false;
            }
        }

        private static void receiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    string buddyIp;
                    bool isBuddyTyping;
                    MessageEventArgs mea = state.m.PumpMessageFromRemote(Encoding.ASCII.GetString(state.buffer, 0, bytesRead), out buddyIp, out isBuddyTyping);

                    if (!String.IsNullOrWhiteSpace(buddyIp))
                        ipBuddyIsTyping[buddyIp] = isBuddyTyping;

                    if (mea != null)
                    {
                        ipBuddyFullNameLookup[mea.FriendIP] = mea.FriendName;
                        OnNewDataReceived(mea);

                        // Update good ip list as needed
                        CSavedIPs.AppendIP(mea.FriendIP);

                        // Signal that all bytes have been received.
                        state.m.ReceiveDone.Set();
                    }

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                }
            }
            catch (Exception e)
            {
                debug(e.ToString());
            }
        }

        static private void send(Socket client, String data, CMessageHandler m)
        {
            // Create the state object.
            StateObject state = new StateObject(m);
            state.workSocket = client;

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(sendCallback), state);
        }

        private static void sendCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                // Retrieve the socket from the state object.  
                Socket client = state.workSocket;

                // Complete sending the data to the remote device.  
                client.EndSend(ar);

                // Signal that all bytes have been sent.  
                state.m.SendDone.Set();
            }
            catch (Exception e)
            {
                debug(e.ToString());
            }
        }

        static public bool SendToBuddy(string myName, bool forceReconnect, string buddyIp, string buddyFullName, string msg, Socket client)
        {
#if !DEBUG_LOOPBACK
            if (buddyIp == myIP)
                return true; // just ignore this situation
#endif
            string fullMsg = CMessageHandler.GenerateMessage(myName, myIP, msg);

            if (client != null)
            {
                // this is a new connection!
                if (!ipBuddyMessages.ContainsKey(buddyIp))
                    ipBuddyMessages.Add(buddyIp, new Queue<string>());
                ipBuddyMessages[buddyIp].Enqueue(fullMsg);

                if (!String.IsNullOrEmpty(buddyFullName))
                    ipBuddyFullNameLookup[buddyIp] = buddyFullName;

                debug("Client starting for " + buddyIp);
                Task.Factory.StartNew(() => StartClientSide(client, buddyIp));
            }
            else
            {
                if (ipsInUse.Contains(buddyIp))
                {
                    lock (ipBuddyMessages[buddyIp])
                    {
                        ipBuddyMessages[buddyIp].Enqueue(fullMsg);
                    }
                }
                else
                {
//#if !DEBUG_LOOPBACK
                    //Task.Factory.StartNew(() => StartClientSide(buddyIp));
//#endif
                    return false; // signal that this message did not go thru
                }
            }
            return true;
        }

        static public string GetIPAddress(string startWith)
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().StartsWith(startWith))
                        localIP = ip.ToString();
                }
            }
            return localIP;
        }

        static private Chatter _debug = null;
        static public void debug(string msg)
        {
#if (DEBUG)
            if (_debug != null)
                _debug.InjectTestMessage("{ " + msg + " }");
#endif
        }

        // ManualResetEvent instance signal completion.
        static private void connectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                debug("Socket connected to " + client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                SendToBuddy(myPCName, true, client.RemoteEndPoint.ToString().Split(':')[0], "", "Connected!", client);
            }
            catch //(Exception e)
            {
                //debug(e.ToString());
            }
        }

        static public void StartSearching(Chatter me)
        {
            _debug = me;

            Task.Factory.StartNew(() =>
            {
            beginThread();

#if DEBUG_LOOPBACK
                debug("Loopback enabled, testing");
#endif

                debug("Search thread launched");

                string[] octs = myIP.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> subsToUse = new List<string>(subsList.Split(','));

                subsToUse.ForEach(debug);

                // protect search function from invalid subnet parts
                bool ok = true;
                foreach (string sub in subsToUse)
                {
                    if (sub.Split('.').Count() != 3)
                    {
                        ok = false;
                        break;
                    }
                    foreach (string o in sub.Split('.'))
                    {
                        foreach (char c in o)
                            if (c < '0' || c > '9')
                            {
                                ok = false;
                                break;
                            }
                        if (ok)
                        {
                            if (Convert.ToInt32(o) > 255)
                                ok = false;
                        }
                    }
                }

                if (ok)
                {
                    int theSubIndex = 0;

                    int subsubMe = Convert.ToInt32(octs[3]);

                    string subPart = subsToUse[theSubIndex++].Trim();
                    int subsubStart = 2;

#if DEBUG_LOOPBACK
                    subsubStart = subsubMe - 1; // give it a little more time
#endif
                    string ipp = "";
                    int index = 0;
                    bool usingPreviousList = true;
                    int counts = 0;

                    while (stayAlive)
                    {
                        using (TcpClient tcpClient = new TcpClient())
                        {
                            try
                            {
                                if (usingPreviousList)
                                {
                                    if (CSavedIPs.PreviousIPList.Count > index)
                                    {
                                        ipp = CSavedIPs.PreviousIPList[index++];
                                    }
                                    else
                                    {
                                        index = 0;
                                        usingPreviousList = false;
                                    }
                                }

                                if (!usingPreviousList)
                                {
                                    ipp = subPart + "." + subsubStart;
                                }
#if DEBUG_LOOPBACK
                                if (!ipsInUse.Contains(ipp))
#else
                                if (ipp != myIP && !ipsInUse.Contains(ipp))
#endif
                                {
                                    // Create a TCP/IP socket.
                                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                                    var result = client.BeginConnect(ipp, Convert.ToInt32(Port), new AsyncCallback(connectCallback), client);

                                    if (result.AsyncWaitHandle.WaitOne(800))
                                    {
                                        debug("Found buddy @" + ipp);
                                    }
                                }
                            }
                            catch { }

                            if (!usingPreviousList)
                            {
                                counts++;
                                if (CSavedIPs.PreviousIPList.Count > 0 && counts >= 13)
                                {
                                    counts = 0;
                                    usingPreviousList = true;
                                    continue; // try IP list again
                                }

                                subsubStart++;
                                if (subsubStart == 127)
                                    subsubStart++;
                                if (subsubStart >= 254)
                                {
                                    subsubStart = 2;

                                    if (theSubIndex >= subsToUse.Count())
                                        break; // stop searching!
                                    subPart = subsToUse[theSubIndex++].Trim();
                                }
                            }
                        }
                    }
                }
                else
                {
                    debug("Search invalid subnet(s)");
                }

                endThread();

                me.EnableGoButton();

                return;
            });
        }
    }

    // State object for receiving data from remote device.  
    public class StateObject
    {
        public StateObject(CMessageHandler mh)
        { m = mh; }
        public CMessageHandler m = null;
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
}