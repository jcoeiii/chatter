using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace chatter
{
    public class CSavedIPs
    {
        static public List<string> PreviousIPList = new List<string>();
        static public string Subs = "";

        static public void ChangeSubList(string subs)
        {
            Subs = subs;
            UpdateFile();
        }

        static public void AppendIP(string ip)
        {
            if (!PreviousIPList.Contains(ip))
            {
                PreviousIPList.Add(ip);
                UpdateFile();
            }
        }

        static private void UpdateFile()
        {
            string[] lines = new string[1 + PreviousIPList.Count];
            lines[0] = Subs;
            int i = 0;
            foreach (string item in PreviousIPList)
                lines[1 + i++] = item;
            try
            {
                if (File.Exists("myips.txt"))
                    File.Delete("myips.txt");
                File.WriteAllLines("myips.txt", lines);
            }
            catch { }
        }

        static public void ReadFile()
        {
            try
            {
                if (File.Exists("myips.txt"))
                {
                    string[] lines = File.ReadAllLines("myips.txt");
                    Subs = lines[0];

                    PreviousIPList.Clear();
                    for (int i = 1; i < lines.Count(); i++)
                        PreviousIPList.Add(lines[i]);
                }
            }
            catch { }
        }
    }
}
