using System;
using System.Collections.Generic;
using System.IO;
using System.Data.Odbc;
using System.Text;

namespace TrunkingDataCollect2
{
    
    class Program
    {
        static OdbcConnection connection;

        static void Main(string[] args)
        {
            string connectionString = @"Driver={Microsoft Access Driver (*.mdb)};" +
     @"Dbq=C:\documents and settings\erikla\my documents\trunktracking.mdb;Uid=Admin;Pwd=;";


            connection = new OdbcConnection(connectionString);
            connection.Open();

            StreamReader stdin = new StreamReader(Console.OpenStandardInput());
            State state = new State();

            do
            {
                string line = stdin.ReadLine();
                State.currentState.doCommand(line);

            } while (stdin.EndOfStream == false);
            connection.Close();
        }

        public static void WriteToDB(Affiliation a)
        {
            string commandString =
                String.Format(
                "insert into affiliation(affiliated, [datetime], [milliseconds], radio, [group]) " +
                "values ({0}, '{1}', {2}, {3}, {4})",
                a.kind == AffiliationType.affiliation ? 1 : 0,
                a.time, a.time.Millisecond, a.radio, a.group);

            //Console.WriteLine(commandString);
            OdbcCommand command = new OdbcCommand(commandString, connection);
            command.ExecuteNonQuery();

        }
        public static void WriteToDB(Call c)
        {
            string commandString =
                String.Format(
                "insert into call([dateTime], [milliseconds], channel, radio, [group]) " + 
                "values ('{0}', {1}, {2}, {3}, {4})",
                c.time, c.time.Millisecond, c.channel, c.radio, c.group);

            //Console.WriteLine(commandString);
            OdbcCommand command = new OdbcCommand(commandString, connection);
            command.ExecuteNonQuery();


            

        }
 
    }
}
