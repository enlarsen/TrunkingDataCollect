using System;
using System.Collections.Generic;
using System.Text;

namespace TrunkingDataCollect2
{
    enum AffiliationType
    {
        unknown = 0,
        affiliation,
        deaffiliation
    }

    class Affiliation
    {
        public AffiliationType kind;
        public DateTime time;
        public int group;
        public int radio;

        public Affiliation(AffiliationType kind, int group, int radio)
        {
            this.kind = kind;
            this.time = DateTime.Now;
            this.group = group;
            this.radio = radio;

        }
    }

    public class OSW
    {
        public int group;
        public string callType;
        public int command;

        public OSW(string group, string callType, string command)
        {
            this.group = Convert.ToInt32(group, 16);
            this.callType = callType;
            this.command = Convert.ToInt32(command, 16);
        }
        public OSW()
        {
        }
    }

    public class Call
    {
        public int group;
        public int radio;
        public DateTime time;
        public int channel;

        public Call(int group, int radio, int channel)
        {
            this.group = group;
            this.radio = radio;
            this.channel = channel;
            this.time = DateTime.Now;
        }
    }
}
