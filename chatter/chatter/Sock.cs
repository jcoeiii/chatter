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
        static private int Port = 25011;

        static private int threadRunningCount = 0;
        static private bool stayAlive = true;
        static private string subsList;
        static private string myIP;
        static private string myPCName;
        static private Dictionary<string, string> ipBuddyMessages = new Dictionary<string, string>();
        static private Dictionary<string, string> ipBuddyFullNameLookup = new Dictionary<string, string>();
        static private List<string> ipsInUse = new List<string>();

        static public event NewDataReceivedEventHandler NewData;
        public delegate void NewDataReceivedEventHandler(MessageEventArgs e);

        static protected void OnNewDataReceived(MessageEventArgs e)
        {
            NewDataReceivedEventHandler handler = NewData;

#if !DEBUG_LOOPBACK
            if (e.FriendIP == myIP)
                return;
#endif
            lock (handler)
            {
                if (handler != null)
                {
                    handler(e);
                }
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
                //IP = IPAddress.Parse(ip);
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
        }

        static void endThread()
        {
            threadRunningCount--;
        }

        static private void StartListening(IPHostEntry ipHostInfo)
        {
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

                    string last = "";

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
                            continue;
                        }
#endif
                        if (last == buddyIp)
                        {
                            debug("SSListener started for " + buddyIp);
                            Task.Factory.StartNew(() => StartServerSide(handler, buddyIp));
                            last = "";
                        }
                        last = buddyIp;
                    }
                }
                catch { }

                Thread.Sleep(200);
            }
        }

        static private void StartServerSide(Socket handler, string buddyIp)
        {
            beginThread();

            byte[] bytes;
            int waitingCount = 7;

            try
            {
                CMessageHandler m = new CMessageHandler();
                int timeout = 0;

//#if !DEBUG_LOOPBACK
                if (!checkForRunningThread(buddyIp))
                {
                    endThread();
                    return;
                }
                //#endif

                if (!ipBuddyMessages.ContainsKey(buddyIp))
                    ipBuddyMessages[buddyIp] = "";

                // An incoming connection needs to be processed.  
                while (stayAlive)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    if (bytesRec == 0)
                        timeout++;
                    else
                        timeout = 0;

                    if (timeout >= 7)
                    {
                        debug("Server timedout for " + buddyIp);
                        break;
                    }

                    MessageEventArgs mea = m.PumpMessageFromRemote(bytes, bytesRec);
                    if (mea != null)
                    {
                        ipBuddyFullNameLookup[mea.FriendIP] = mea.FriendName;
                        OnNewDataReceived(mea);
                    }

                    if (waitingCount <= 0 && !String.IsNullOrEmpty(ipBuddyMessages[buddyIp]))
                    {
                        m.AddMessageToSend(ipBuddyMessages[buddyIp]);
                        ipBuddyMessages[buddyIp] = "";
                    }

                    CMessageHandler.MsgState state = m.ProcessStates();

                    // Write to Client as needed
                    if (state == CMessageHandler.MsgState.ReadyForRemote)
                    {
#if (DEBUG)
                        m.InjectTestMessage(";S" + timeout);
#endif
                        debug("Server sent out msg to " + buddyIp);

                        byte[] msg = m.MessageReady;
                        // Send back a response.
                        handler.Send(msg, msg.Length, SocketFlags.None);
                    }
                    else
                    {
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(" ");
                        // Send back a response.
                        handler.Send(msg, msg.Length, SocketFlags.None);
                    }

                    if (waitingCount > 0)
                        waitingCount--;

                    Thread.Sleep(500);
                    CSavedIPs.AppendIP(buddyIp);
                }
            }
            catch (Exception e)
            {
                debug("Server exception for " + buddyIp + e.ToString());
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

        static public void StartClientSide(string buddyIp)
        {
            beginThread();

            int timeout = 0;
            TcpClient client = null;

            try
            {
                // Buffer for reading data
                Byte[] bytes;

#if !DEBUG_LOOPBACK
                if (!checkForRunningThread(buddyIp))
                {
                    endThread();
                    return;
                }
#endif
                if (!ipBuddyMessages.ContainsKey(buddyIp))
                    ipBuddyMessages[buddyIp] = "";

                client = new TcpClient(buddyIp, Port);
                NetworkStream stream = client.GetStream();
                CMessageHandler m = new CMessageHandler();

                // Enter the working loop
                while (stayAlive)
                {
                    // Get a stream object for reading and writing
                    //NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 1500;

                    // Loop to receive all the data sent by the client.
                    try
                    {
                        //if (stream.CanRead)
                        //{
                        bytes = new byte[2048];
                        int bytesRec = stream.Read(bytes, 0, bytes.Length);
                        timeout = 0;
                        MessageEventArgs mea = m.PumpMessageFromRemote(bytes, bytesRec);

                        if (mea != null)
                        {
                            ipBuddyFullNameLookup[mea.FriendIP] = mea.FriendName;
                            OnNewDataReceived(mea);
                        }
                        //}
                    }
                    catch (IOException)
                    {
                        timeout++;
                    }

                    if (timeout >= 7)
                    {
                        debug("Client timedout for " + buddyIp);
                        break;
                    }

                    if (!String.IsNullOrEmpty(ipBuddyMessages[buddyIp]))
                    {
                        m.AddMessageToSend(ipBuddyMessages[buddyIp]);
                        ipBuddyMessages[buddyIp] = "";
                    }

                    CMessageHandler.MsgState state = m.ProcessStates();

                    // Write to Client as needed
                    if (state == CMessageHandler.MsgState.ReadyForRemote)
                    {
#if (DEBUG)
                        m.InjectTestMessage(":C" + timeout);
#endif
                        debug("Client sent out msg to " + buddyIp);

                        byte[] msg = m.MessageReady;
                        // Send back a response.
                        stream.Write(msg, 0, msg.Length);
                    }
                    else
                    {
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(" ");
                        // Send back a response.
                        stream.Write(msg, 0, msg.Length);
                    }

                    Thread.Sleep(500);
                    CSavedIPs.AppendIP(buddyIp);
                }

                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch (Exception e)
            {
                debug("Client exception for " + buddyIp + e.ToString());
            }

            
            // Shutdown and end connection
            if (client != null)
                client.Close();

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

        static public bool SendToBuddy(string myName, TcpClient client, string buddyIp, string buddyFullName, string msg)
        {
#if !DEBUG_LOOPBACK
            if (buddyIp == myIP)
                return true; // just ignore this situation
#endif
            string fullMsg = CMessageHandler.GenerateMessage(myName, myIP, msg);

            if (client != null)// && !ipsInUse.Contains(buddyIp))
            {
                // this is a new connection!
                ipBuddyMessages[buddyIp] = fullMsg;
                if (!String.IsNullOrEmpty(buddyFullName))
                    ipBuddyFullNameLookup[buddyIp] = buddyFullName;

                debug("Client starting for " + buddyIp);
                Task.Factory.StartNew(() => StartClientSide(buddyIp));
            }
            else
            {
                if (ipsInUse.Contains(buddyIp))
                {
                    ipBuddyMessages[buddyIp] = fullMsg;
                }
                else
                {
                    // try to restart connection if possible!
#if !DEBUG_LOOPBACK
                    Task.Factory.StartNew(() => StartClientSide(buddyIp));
#endif
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

        static public void StartSearching(Chatter me)
        {
            _debug = me;

            Task.Factory.StartNew(() =>
            {
                beginThread();

                string[] octs = myIP.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                string[] subsToUse = subsList.Split(',');

                int theSubIndex = 0;

                int subsubMe = Convert.ToInt32(octs[3]);

                string subPart = subsToUse[theSubIndex++];
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
                                var result = tcpClient.BeginConnect(ipp, Convert.ToInt32(Port), null, null);

                                if (result.AsyncWaitHandle.WaitOne(800))
                                {
                                    debug("Found buddy @" + ipp);

                                    // found a buddy, send message
                                    SendToBuddy(myPCName, tcpClient, ipp, "", "Connected!");
                                    //CSavedIPs.AppendIP(ipp);
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
                                subPart = subsToUse[theSubIndex++];
                            }

                            

                        }
                    }
                }

                endThread();

                me.EnableGoButton();

                return;
            });
        }
    }
}
