using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TrunkingDataCollect2
{
    public delegate void processCommand(OSW osw);

    class State
    {
        public static Dictionary<int, Affiliation> affiliations =
            new Dictionary<int, Affiliation>();
        public static Dictionary<int, Call> calls =
            new Dictionary<int, Call>();

        protected Dictionary<string, processCommand>
            commands;

        public static State currentState;
        public static OSW lastOSW;

        public static StateNormal stateNormal = new StateNormal();
        public static StateBad stateBad = new StateBad();
        public static State308 state308 = new State308();

        public virtual void receivedBad(OSW osw) 
        { 
            //Console.WriteLine("Bad"); 
            currentState = stateBad; 
        }
        public virtual void received308(OSW osw) 
        { 
            //Console.WriteLine("308"); 
            lastOSW = osw; 
            currentState = state308; 
        }
        public virtual void received310(OSW osw) 
        { 
            //Console.WriteLine("***310"); 
            currentState = stateNormal; 
        }
        public virtual void received30b(OSW osw) 
        { 
            //Console.WriteLine("30b"); 
            currentState = stateNormal; 
        }
        public virtual void receivedFrequency(OSW osw) 
        { 
            //Console.WriteLine("Freq."); 
            currentState = stateNormal; 
        }
        public virtual void receivedOther(OSW osw) 
        { 
            //Console.WriteLine("Other: cmd: {0:X4}, group: {1:X4}", osw.command, osw.group); 
            currentState = stateNormal; 
        }

        public State()
        {
            currentState = stateNormal;
            commands = new Dictionary<string, processCommand>();

            commands.Add("bad", receivedBad);
            commands.Add("0308", received308);
            commands.Add("0310", received310);
            commands.Add("030b", received30b);

        }
        
        public void doCommand(string line)
        {
            OSW osw;
            Match match;
            processCommand commandDelegate;
            int command;

            if (line.ToLower().Contains("bad") == true)
            {
                commandDelegate = receivedBad;
                commandDelegate(null);
                return;
            }
            match = Regex.Match(line,
                @"(?<group>[0-9a-f]{4}) (?<callType>[ig]) (?<command>[0-9a-f]{4})",
                RegexOptions.IgnoreCase);

            if (match == null)
                return;

            
            osw = new OSW(match.Groups["group"].Value,
                match.Groups["callType"].Value,
                match.Groups["command"].Value);

         //   Console.WriteLine("CMD PROC: command: {0:X4} group: {1:X4}",
         //       osw.command, osw.group);

            command = Convert.ToInt32(match.Groups["command"].Value, 16);

            if (commands.ContainsKey(match.Groups["command"].Value))
            {
                commandDelegate = commands[match.Groups["command"].Value];


            }
            else
            {
                if (isFrequency(command) == true)
                {
                    commandDelegate = receivedFrequency;
                }
                else
                {
                    commandDelegate = receivedOther;
                }
            }
            commandDelegate(osw);

        }


        static bool isFrequency(int channel)
        {
            if (channel >= 0 && channel <= 0x2f7)
                return true;
            if (channel >= 0x32f && channel <= 0x33f)
                return true;
            if (channel >= 0x3c1 && channel <= 0x3fe)
                return true;

            return false;
        }

    }

    class StateNormal : State
    {
 /*       public StateNormal()
        {
            commands = new Dictionary<string, processCommand>();

            commands.Add("bad", receivedBad);
            commands.Add("0308", received308);
            commands.Add("0310", received310);
            commands.Add("030b", received30b);
        }
*/ 
    }
    
    class StateBad : State
    {

 /*       public StateBad()
        {
            commands = new Dictionary<string, processCommand>();

            commands.Add("bad", receivedBad);
            commands.Add("0308", received308);
            commands.Add("0310", received310);
            commands.Add("030b", received30b);
        }
 */        
    }
    class State308 : State
    {
 /*       public  State308()
        {
            commands = new Dictionary<string, processCommand>();

            commands.Add("bad", receivedBad);
            commands.Add("0308", received308);
            commands.Add("0310", received310);
            commands.Add("030b", received30b);

        } */  

        public override void received308(OSW osw)
        {
            currentState = stateNormal;
        }

        public override void received310(OSW osw)
        {
            int radio = lastOSW.group;
            int group = osw.group;

            if ((group & 0x000f) != 0x0a)
            {
                StreamWriter sw = new StreamWriter("odd.txt", true);
                sw.WriteLine("Found odd group: {0:X4}", group);
                sw.Close();
            }
            group &= 0xfff0;

            if (affiliations.ContainsKey(radio))
            {
                if (affiliations[radio].kind == AffiliationType.affiliation)
                {
                    if (affiliations[radio].group == group)
                    {
                        currentState = stateNormal;
                        return;
                    }
                    else
                    {
                        affiliations.Remove(radio);
                    }
                }
                else
                {
                    if (affiliations[radio].kind == AffiliationType.deaffiliation)
                    {
                        affiliations.Remove(radio);
                    }
                }
            }

            Affiliation a = new Affiliation(AffiliationType.affiliation,
                group, lastOSW.group);
            Program.WriteToDB(a);
            affiliations.Add(radio, a);
            Console.WriteLine("Radio {0} aff--> {1}", radio, group);
            //Console.WriteLine("Received 308/310");
            currentState = stateNormal;
        }

        public override void received30b(OSW osw)
        {
            int radio = lastOSW.group;

            if (affiliations.ContainsKey(radio) &&
                affiliations[radio].kind == AffiliationType.affiliation)
            {
                affiliations[radio].kind = AffiliationType.deaffiliation;
                affiliations[radio].time = DateTime.Now;
                Console.WriteLine("{0} deaff.", radio);
                Program.WriteToDB(affiliations[radio]);

            }
            //Console.WriteLine("Received 308/30b");
            currentState = stateNormal;
        }

        public override void receivedFrequency(OSW osw)
        {
            int channel = osw.command;
            int radio = lastOSW.group;
            int group = osw.group & 0xfff0;

            if (calls.ContainsKey(group) == true)
            {
                if (calls[group].radio == radio)
                {
                    if (calls[group].time < DateTime.Now.AddSeconds(-2))
                    {
                        calls[group].time = DateTime.Now;
                        calls[group].channel = channel;
                        Program.WriteToDB(calls[group]);
                    }
                    else
                    {
                        currentState = stateNormal;
                        return;
                    }

                }
                else
                {
                    calls[group].radio = radio;
                    calls[group].channel = channel;
                    calls[group].time = DateTime.Now;
                    Program.WriteToDB(calls[group]);
                }
            }

            else
            {
                Call c = new Call(group, radio, channel);
                calls.Add(group, c);
                Program.WriteToDB(c);
            }

            Console.WriteLine("Call on channel: {0} radio: {1} --> group: {2}", osw.command,
                lastOSW.group, osw.group & 0xfff0);
            //Console.WriteLine("Received 308/Freq");
            currentState = stateNormal;
        }



    }

 

}
