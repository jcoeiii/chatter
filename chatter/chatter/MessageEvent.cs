using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatter
{
    public class MessageEventArgs : EventArgs
    {
        private string id;
        private string friendName;
        private string friendIP;
        private string textFromFriend;
        private bool valid = false;

        private bool isFile = false;
        private string fileName;
        private string fileData;
        private int chunkId = 0;
        private string checksum;

        public MessageEventArgs(string data)
        {
            if (data != null && data.Contains("|"))
            {
                data = data.Trim(); // important trim
                int loc = data.IndexOf('|');
                if (loc != -1)
                {
                    int loc1 = data.IndexOf('|', loc + 1);
                    if (loc1 > 0)
                    {
                        int loc2 = data.IndexOf('|', loc1 + 1);
                        if (loc2 > 0)
                        {
                            int loc3 = data.IndexOf('|', loc2 + 1);
                            if (loc3 > 0)
                            {
                                id = data.Substring(loc + 1, loc1 - loc - 1);
                                friendName = data.Substring(loc1 + 1, loc2 - loc1 - 1);
                                friendIP = data.Substring(loc2 + 1, loc3 - loc2 - 1);
                                textFromFriend = data.Substring(loc3 + 1, data.Length - loc3 - 1);

                                checksum = Sock.Checksum(textFromFriend.Replace("<EOF>", ""));
                                
                                if (textFromFriend.EndsWith("<EOF>"))
                                {
                                    if (textFromFriend.StartsWith("<OBJECT>FILE"))
                                    {
                                        isFile = true;
                                        try
                                        {
                                            string[] splitObj = textFromFriend.Split('*');
                                            this.chunkId = Convert.ToInt32(splitObj[1]);
                                            this.fileName = splitObj[2];
                                            this.fileData = (splitObj[3].Substring(0, splitObj[3].Length - 4 - 1));
                                            valid = true;
                                        }
                                        catch
                                        {
                                            valid = false;
                                        }
                                        textFromFriend = textFromFriend.Replace("<EOF>", "");
                                        return;
                                    }

                                    textFromFriend = textFromFriend.Replace("<EOF>", "");
                                    valid = true;
                                }
                                else
                                {
                                    textFromFriend = "";
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool Valid { get { return this.valid; } }
        public int ChunkId {  get { return this.chunkId;  } }
        public bool IsAck { get; set; }
        public string Id { get { return this.id; } }
        public string FriendName { get { return this.friendName; } }
        public string FriendIP { get { return this.friendIP; } }
        public string TextFromFriend { get { return this.textFromFriend; } }

        public bool IsFile {  get { return this.isFile; } }
        public string FileName {  get { return this.fileName;  } }
        public string FileData { get { return this.fileData; } }
        public string Checksum { get { return this.checksum; } }
    }

}
