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
        private byte[] fileData;

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

                                if (textFromFriend.EndsWith("<EOF>"))
                                {
                                    if (textFromFriend.StartsWith("<OBJECT>FILE"))
                                    {
                                        string[] splitObj = textFromFriend.Split('\t');
                                        this.fileName = splitObj[1];
                                        this.fileData = StringCompressor.ToHexBytes(splitObj[2].Substring(0, splitObj[2].Length - 4 - 1));

                                        isFile = true;
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

        public string Id { get { return this.id; } }
        public string FriendName { get { return this.friendName; } }
        public string FriendIP { get { return this.friendIP; } }
        public string TextFromFriend { get { return this.textFromFriend; } }

        public bool IsFile {  get { return this.isFile; } }
        public string FileName {  get { return this.fileName;  } }
        public byte[] FileData { get { return this.fileData; } }
    }

}
