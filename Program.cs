using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Input;
using System.Windows;
using System.Text;

namespace minecraft_restarter
{
    internal static class Program
    {
        private static int lineCount = 0;
        private static StringBuilder output = new StringBuilder();
        private static DateTime timeOfLastRestart = DateTime.UtcNow;
        private static int memsize = 0; // memsize in KB
        public static void Main()
        {
            string command = "";
            Process ServerProc = new Process();
            ServerStart(ServerProc);
            _ = CheckMemoryUse(ServerProc);

            while (true)
            {

                bool serverUp = IsServer_up();
                bool outOfMemory = CheckOutOfMemory();

                if (outOfMemory)
                {
                    ServerStop(ServerProc);
                    break;
                }

                if (serverUp)
                {
                    Console.WriteLine("server up since "+ timeOfLastRestart+"UTC");
                    Console.WriteLine("Memory used " + (memsize/1024)+ "MB");
                    Console.WriteLine("Uptime is " + (DateTime.UtcNow - timeOfLastRestart));
                    ///Thread.Sleep(5000);
                    command = AskForExit(command, ServerProc);
                }
                else
                {
                    Console.WriteLine("server down, restarting server");
                    ServerStart(ServerProc);
                }

            }
        }

        private static string AskForExit(string command, Process ServerProc)
        {
            try
            {
                Console.WriteLine("write stop in the next 10 seconds to stop the server");
                command = Reader.ReadLine(10000);
                if (command == "stop")
                {
                    ServerStop(ServerProc);
                    Environment.Exit(0);
                }
            }
            catch (TimeoutException)
            {
                //Console.WriteLine("Sorry, you waited too long.");
            }

            return command;
        }

        private static void ServerStart(Process ServerProc)
        {

            string ServerFile;
            string ServerPath;

            ServerFile = "server.jar";
            ServerPath = @"C:\MC\The Paper World\";

            var startInfo = new ProcessStartInfo("java", "-Xmx6G -Xms6G -jar " + ServerFile + " nogui");
            startInfo.WorkingDirectory = ServerPath;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false; // Necessary for Standard Stream Redirection
            startInfo.CreateNoWindow = true; // You can do either this or open it with "javaw" instead of "java"
            ServerProc.EnableRaisingEvents = true;
            ServerProc.StartInfo = startInfo;
            ServerProc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                // Prepend line numbers to each line of the output.
                if (!String.IsNullOrEmpty(e.Data))
                {
                    lineCount++;
                    output.Append("\n[" + lineCount + "]: " + e.Data);
                }
            });
            ServerProc.Start();
            ServerProc.BeginErrorReadLine();
            //ServerProc.BeginOutputReadLine();
            Console.WriteLine(output);
            timeOfLastRestart = DateTime.UtcNow;
            Thread.Sleep(10000);

        }

        private static void ServerStop(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            String inputText;

            //Countdown(myStreamWriter);

            inputText = "stop";
            myStreamWriter.WriteLine(inputText);
            myStreamWriter.Close();
            Thread.Sleep(10000); //Tam said it must be ten
            bool serverUp = IsServer_up();
            if (serverUp)
            {
                ServerProc.Kill();
            }
        }

        private static string Countdown(StreamWriter myStreamWriter)
        {
            string inputText = "say server restart in 1 minute";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 45 seconds left, lazy potato, get off before it restarts!";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 30 seconds left, don't be a fool, flee while you still can";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 15 seconds left, THIS IS NOT A DRILL";
            Thread.Sleep(5000);
            for (int a = 10; a >= 0; a--)
            {
                myStreamWriter.WriteLine("say server restart in {0}", a);
                Thread.Sleep(1000);
            }

            return inputText;
        }
        public static int CheckMemoryUse(Process ServerProc)
        {
            Process proc = ServerProc;
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = proc.ProcessName;
            memsize = Convert.ToInt32(PC.NextValue()) / (int)(1024);
            PC.Close();
            PC.Dispose();
            return memsize;
        }


        public static bool CheckOutOfMemory()
        {

            String myEventType = null;
            // Associate the instance of 'EventLog' with local System Log.
            EventLog myEventLog = new EventLog("System", ".");
            int myOption = Convert.ToInt32(3);
            switch (myOption)
            {
                case 1:
                    myEventType = "Error";
                    break;
                case 2:
                    myEventType = "Information";
                    break;
                case 3:
                    myEventType = "Warning";
                    break;
                default: break;
            }

            EventLogEntryCollection myLogEntryCollection = myEventLog.Entries;
            int myCount = myLogEntryCollection.Count;
            // Iterate through all 'EventLogEntry' instances in 'EventLog'.
            for (int i = myCount - 1; i > -1; i--)
            {
                EventLogEntry myLogEntry = myLogEntryCollection[i];
                // Select the entry having desired EventType.
                if (myLogEntry.EntryType.ToString().Equals(myEventType))
                {
                    // Display Source of the event.
                    Console.WriteLine(myLogEntry.Source
                       + " was the source of last event of type "
                       + myLogEntry.EntryType);
                    if (myLogEntry.Source == "Resource-Exhaustion-Detector" && (timeOfLastRestart.AddHours(1) > DateTime.Now))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //Resource-Exhaustion-Detector

        private static bool IsServer_up()
        {

            var query = "SELECT * "
                    + "FROM Win32_Process "
                    + "WHERE Name = 'java.exe' "
                    + "OR "
                    + "Name = 'javaw.exe'";

            // get associated processes
            List<Process> servers = null;
            using (var results = new ManagementObjectSearcher(query).Get())
                servers = results.Cast<ManagementObject>()
                                 .Select(mo => Process.GetProcessById((int)(uint)mo["ProcessId"]))
                                 .ToList();
            Console.WriteLine(servers.Count);
            if (servers.Count < 1)
            {
                return false;
            }

            return true;
        }

        class Reader
        {
            private static Thread inputThread;
            private static AutoResetEvent getInput, gotInput;
            private static string input;

            static Reader()
            {
                getInput = new AutoResetEvent(false);
                gotInput = new AutoResetEvent(false);
                inputThread = new Thread(reader);
                inputThread.IsBackground = true;
                inputThread.Start();
            }

            private static void reader()
            {
                while (true)
                {
                    getInput.WaitOne();
                    input = Console.ReadLine();
                    gotInput.Set();
                }
            }

            // omit the parameter to read a line without a timeout
            public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
            {
                getInput.Set();
                bool success = gotInput.WaitOne(timeOutMillisecs);
                if (success)
                    return input;
                else
                    throw new TimeoutException("User did not provide input within the timelimit.");
            }
        }
    }
}
