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


namespace minecraft_restarter
{
    class Program
    {

        public static void Main()
        {
            Process ServerProc = new Process();
            ServerStart(ServerProc);

            while (true)
            {

                bool serverUp = IsServer_up();
                bool outOfMemory = CheckOutOfMemory();

                if (outOfMemory == true)
                {

                    ServerStop(ServerProc);

                }

                if (serverUp == true)
                {
                    Console.WriteLine("server up");
                    Thread.Sleep(5000);

                }
                else
                {
                    Console.WriteLine("server down, restarting server");
                    ServerStart(ServerProc);
                    Thread.Sleep(5000);
                }

            }
        }

        private static void ServerStart(Process ServerProc)
        {
            //string path = @"C:\MC\The Paper World\"; //Path to your server.jar file.
            //ServerProc.StartInfo.FileName = path + "Start Server.bat"; //Name of the .jar file.
            //ServerProc.StartInfo.WorkingDirectory = path;
            //ServerProc.StartInfo.UseShellExecute = true;
            //ServerProc.Start();
            string ServerFile;
            string ServerPath;


            // If the values are already there then just load them.
            ServerFile = "server.jar";
            ServerPath = @"C:\MC\The Paper World\";

            var startInfo = new ProcessStartInfo("java", "-Xmx6G -Xms6G -jar " + ServerFile + " nogui");
            //var startInfo = new ProcessStartInfo("java", "-Xmx6G -Xms6G -jar " + ServerFile);
            // Replace the following with the location of your Minecraft Server
            startInfo.WorkingDirectory = ServerPath;
            // Notice that the Minecraft Server uses the Standard Error instead of the Standard Output

            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false; // Necessary for Standard Stream Redirection
            startInfo.CreateNoWindow = true; // You can do either this or open it with "javaw" instead of "java"

            ServerProc = new Process();
            ServerProc.StartInfo = startInfo;
            ServerProc.EnableRaisingEvents = true;
            ServerProc.Start();
            Thread.Sleep(10000);

        }

        private static void ServerStop(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;

            // Prompt the user for input text lines to sort.
            // Write each line to the StandardInput stream of
            // the sort command.
            String inputText;

            inputText = "stop";
            myStreamWriter.WriteLine(inputText);

            // End the input stream to the sort command.
            // When the stream closes, the sort command
            // writes the sorted text lines to the
            // console.
            myStreamWriter.Close();

        }


        private static bool CheckOutOfMemory()
        {
            //if (servers.Count < 1)
            //{
            //    return true;
            //}

            return true;
        }

        private static bool IsServer_up()
        {

            var query = "SELECT * "
                    + "FROM Win32_Process "
                    + "WHERE Name = 'java.exe' "
                    + "OR "
                    + "Name = 'javaw.exe'";
            //+ "AND CommandLine LIKE '%Minecraft%'";

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
    }
}
