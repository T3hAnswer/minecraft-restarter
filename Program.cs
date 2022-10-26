using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Text;

namespace minecraft_restarter
{
    internal static class Program
    {
        private static string lastLine = "";
        private static DateTime timeOfLastRestart = DateTime.UtcNow;
        public static void Main()
        {

            Process ServerProc = new Process();
            ServerStart(ServerProc);

            while (true)
            {

                bool serverUp = IsServer_up();

                if (CheckMemoryUse(ServerProc) / 1024 > 6870)
                {
                    ServerStop(ServerProc);
                    ServerStart(ServerProc);
                }

                if (ServerUptime().TotalHours > 24 && PlayersOnline(ServerProc))
                {
                    ServerStop(ServerProc);
                    ServerStart(ServerProc);
                }

                if (serverUp)
                {
                    Console.WriteLine("server up since " + timeOfLastRestart + "UTC");
                    Console.WriteLine("Memory used " + (CheckMemoryUse(ServerProc) / 1024) + "MB");
                    Console.WriteLine("Uptime is " + (ServerUptime()));
                    AskForCommands(ServerProc);
                }
                else
                {
                    Console.WriteLine("server down, restarting server");
                    ServerStart(ServerProc);
                }

            }
        }

        private static TimeSpan ServerUptime()
        {
            TimeSpan uptime = DateTime.UtcNow.Subtract(timeOfLastRestart);
            return uptime;
        }

        private static void AskForCommands(Process ServerProc)
        {
            try
            {
                Console.WriteLine("write stop in the next 10 seconds to stop the server");
                string command = Reader.ReadLine(15000);
                if (command == "stop")
                {
                    ServerStop(ServerProc);
                    Environment.Exit(0);
                }
                if (command == "restart")
                {
                    ServerStop(ServerProc);
                    ServerStart(ServerProc);
                }

                ServerCommandInputWriter(ServerProc, command);
            }
            catch (TimeoutException)
            {
            }
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
            ServerProc.OutputDataReceived += new DataReceivedEventHandler(ServerOutputDataReceived);
            ServerProc.ErrorDataReceived += new DataReceivedEventHandler(ServerErrorDataReceived);
            ServerProc.Start();

            ServerProc.BeginOutputReadLine();

            timeOfLastRestart = DateTime.UtcNow;
            Thread.Sleep(15000);

        }
        static void ServerErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Error: {0}", e.Data);
        }

        static void ServerOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("{0}", e.Data);
            lastLine = e.Data;
        }

        private static void ServerStop(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            String inputText;

#if RELEASE
            Countdown(myStreamWriter);
#endif
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

        private static Boolean PlayersOnline(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            String inputText;
            string checkFor = "There are 0";
            inputText = "list";
            myStreamWriter.WriteLine(inputText);
            bool b = lastLine.Contains(checkFor);
            return b;
        }

        private static void ServerCommandInputWriter(Process ServerProc, string command)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            myStreamWriter.WriteLine(command);
        }

        private static void Countdown(StreamWriter myStreamWriter)
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
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(5000);
            for (int a = 10; a >= 0; a--)
            {
                myStreamWriter.WriteLine("say server restart in {0}", a);
                Thread.Sleep(1000);
            }
        }

        public static int CheckMemoryUse(Process ServerProc)
        {
            Process proc = ServerProc;
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = proc.ProcessName;
            int memsize = Convert.ToInt32(PC.NextValue()) / (int)(1024);
            PC.Close();
            PC.Dispose();
            return memsize;
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
