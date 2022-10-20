using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void Main()
        {
            string command = "";
            Process ServerProc = new Process();
            
            ServerStart(ServerProc);

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
                    Console.WriteLine("server up");
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
                Console.WriteLine("write stop in the next 5 seconds to stop the server");
                command = Reader.ReadLine(5000);
                if (command == "stop")
                {
                    ServerStop(ServerProc);
                    Environment.Exit(0);
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Sorry, you waited too long.");
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
            //ServerProc = new Process();
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
            //Thread.Sleep(10000);
            ServerProc.BeginOutputReadLine();
            Console.WriteLine(output);


            Thread.Sleep(10000);

        }

        private static void ServerStop(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            String inputText;
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

        public static bool CheckOutOfMemory()
        {


            string eventLogName = "System";

            EventLog eventLog = new EventLog();
            eventLog.Log = eventLogName;

            foreach (EventLogEntry log in eventLog.Entries)
            {
                Console.WriteLine("{0}\n", log.Message);
            }


            return false;
        }

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
