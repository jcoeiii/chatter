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

        public MessageEventArgs(string data)
        {
            if (data != null && data.Contains("|"))
            {
                data = data.Trim(); // important trim
                int loc = data.IndexOf('|');
                if (loc != -1)
                {
                    int loc2 = data.IndexOf('|', loc + 1);
                    if (loc2 > 0)
                    {
                        int loc3 = data.IndexOf('|', loc2 + 1);
                        if (loc3 > 0)
                        {
                            id = data.Substring(0, loc);
                            friendName = data.Substring(loc + 1, loc2 - loc - 1);
                            friendIP = data.Substring(loc2 + 1, loc3 - loc2 - 1);
                            textFromFriend = data.Substring(loc3 + 1, data.Length - loc3 - 1);

                            if (textFromFriend.EndsWith("<EOF>"))
                                valid = true;

                            textFromFriend = textFromFriend.Replace("<EOF>", "");
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
    }

}
