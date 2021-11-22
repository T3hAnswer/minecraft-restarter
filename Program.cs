using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace minecraft_restarter
{
    class Program
    {
        static void Main()
        {
            while (true)
            {

                bool serverUp = Server_check();

                if (serverUp == true)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("server up");
                }
                else
                {
                    Console.WriteLine("server down, restarting server");
                    RestartServer();
                    Thread.Sleep(1000);
                }

            }
        }

        private static void RestartServer()
        {
            string path = @"C:\MC\"; //Path to your server.jar file.
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = path + "server.jar"; //Name of the .jar file.
            process.StartInfo.WorkingDirectory = path;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        private static bool Server_check()
        {

            var query ="SELECT * "
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
        /*
        private string GetJavaInstallationPath()
        {
            string environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(environmentPath))
            {
                return environmentPath;
            }
            string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
            if (!Environment.Is64BitOperatingSystem)
            {
                using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                {
                    string currentVersion = rk.GetValue("CurrentVersion").ToString();
                    using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                    {
                        return key.GetValue("JavaHome").ToString();
                    }
                }
            }
            else
            {
                using (var view64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                                                            RegistryView.Registry64))
                {
                    using (var clsid64 = view64.OpenSubKey(javaKey))
                    {
                        string currentVersion = clsid64.GetValue("CurrentVersion").ToString();
                        using (RegistryKey key = clsid64.OpenSubKey(currentVersion))
                        {
                            return key.GetValue("JavaHome").ToString();
                        }
                    }
                }
            }

        }
        */

        private static void FileMover(string sourcePath, string targetPath)
        {
            string sourceDirectory = sourcePath;
            try
            {
                var txtFiles = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".log") || s.StartsWith("hs"));

                foreach (string currentFile in txtFiles)
                {
                    string archiveDirectory = targetPath;
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(currentFile);
                    string extension = Path.GetExtension(currentFile);
                    DateTime dateTime = File.GetLastWriteTime(currentFile);
                    FileInfo file =
                        new FileInfo(archiveDirectory + "/" + dateTime.ToString("dd-MM-yyyy") + "/" +
                                     fileNameOnly + extension);
                    file.Directory?.Create();

                    string newFullPath = file.FullName;

                    while (File.Exists(newFullPath))
                    {
                        string tempFileName = $"{fileNameOnly}_{count++}";
                        newFullPath = Path.GetDirectoryName(newFullPath);
                        newFullPath = Path.Combine(newFullPath!, tempFileName + extension);
                    }
                    Directory.Move(currentFile, newFullPath);
                }
            }
            catch (IOException)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + $"Destination or source folder not found");
                //Following part would help keep application running if someone accidentally renamed/deleted/moved the folders,
                //but any existing logs would obviously not carry over.
                Console.WriteLine("Attempting to recreate folders");
                Directory.CreateDirectory(targetPath);
                Directory.CreateDirectory(sourcePath);
            }
        }
    }
}
