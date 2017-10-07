using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.Management;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace VoliBot
{
    class LeagueMonitor
    {
        private static string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoliBot");
        public Process lcuProcess { get; private set; }
        public int lcuPort { get; private set; }
        public string lcuPassword = "RWr6Cf-tOJkKA768wjRl6A";

        static LeagueMonitor()
        {
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        }

        /**
         * Utility method that calls the specified argument functions whenever League starts or stops.
         * This is done by observing the lockfile, and the start function gets the contents of the lockfile as a param.
         */
        public LeagueMonitor(string lcuExecutablePath, Action<int, string> onStart, Action onStop)
        {
            var leagueDir = Path.GetDirectoryName(lcuExecutablePath) + "\\";
            lcuPort = GetAvailablePort(50000);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = GetLCUPath();
            psi.Arguments = "--headless --app-port=" + lcuPort + " --remoting-auth-token=" + lcuPassword + " --allow-multiple-clients";
            lcuProcess = Process.Start(psi);
            lcuProcess.WaitForExit();
            onStart(lcuPort, lcuPassword);
            lcuProcess.Exited += (o, e) =>
            {
                onStop();
            };

            // Kill the Client, when closing bot.
            Application.ApplicationExit += (o, e) =>
            {
                lcuProcess.Kill();
            };
        }

        /**
         * Either gets the LCU path from the saved properties, or by prompting the user.
         * Returns null if the user does not want to select a folder.
         */
        public static string GetLCUPath()
        {
            string configPath = Path.Combine(dataDir, "lcuPath");
            string path = File.Exists(configPath) ? File.ReadAllText(configPath) : "C:/Riot Games/League of Legends/LeagueClient.exe";

            if (!IsValidLCUPath(path))
            {
                var leaguePath = GetLCUPathWithRunningLeagueClient();
                if (leaguePath == null)
                {
                    // Ask the user to run the League client
                    MessageBox.Show(
                        "Mimic could not find the League client at " + path + ". Make sure the League client runs, and press OK.",
                        "LCU not found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                    );

                    // Now try again
                    leaguePath = GetLCUPathWithRunningLeagueClient();
                }

                path = leaguePath != null ? leaguePath + "LeagueClient.exe" : path;

                // Store choice so we don't have to look for it again.
                if (IsValidLCUPath(path))
                    File.WriteAllText(configPath, path);
            }

            while (!IsValidLCUPath(path))
            {
                // Notify that the path is invalid.
                MessageBox.Show(
                    "Mimic could not find the running League client. Please select the location of 'LeagueClient.exe' manually.",
                    "LCU not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );

                // Ask for new path.
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.Title = "Select LeagueClient.exe location.";
                dialog.InitialDirectory = "C:\\Riot Games\\League of Legends";
                dialog.EnsureFileExists = true;
                dialog.EnsurePathExists = true;
                dialog.DefaultFileName = "LeagueClient";
                dialog.DefaultExtension = "exe";
                dialog.Filters.Add(new CommonFileDialogFilter("Executables", ".exe"));
                dialog.Filters.Add(new CommonFileDialogFilter("All Files", ".*"));
                if (dialog.ShowDialog() == CommonFileDialogResult.Cancel)
                {
                    // User wants to cancel. Exit
                    return null;
                }

                path = dialog.FileName;

                // Store choice so we don't have to ask for it again.
                File.WriteAllText(configPath, path);
            }

            return path;
        }

        /// <summary>
        /// checks for used ports and retrieves the first free port
        /// </summary>
        /// <returns>the free port or 0 if it did not find a free port</returns>
        public static int GetAvailablePort(int startingPort)
        {
            IPEndPoint[] endPoints;
            List<int> portArray = new List<int>();

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                               where n.LocalEndPoint.Port >= startingPort
                               select n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            //getting active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            portArray.Sort();

            for (int i = startingPort; i < UInt16.MaxValue; i++)
                if (!portArray.Contains(i))
                    return i;

            return 0;
        }

        /*
         * Find the league client location by checking for the process, 
         * then look for an argument with the --install-directory,
         * in which the lockfile should be placed.
         */
        public static string GetLCUPathWithRunningLeagueClient()
        {
            var leagueProcesses = Process.GetProcesses().Where(p => p.ProcessName.Contains("League"));
            foreach (var process in leagueProcesses)
            {
                try
                {
                    string commandLine = process.GetCommandLine();
                    var indexOfInstallDirectory = commandLine.IndexOf("--install-directory");
                    if (indexOfInstallDirectory == -1)
                        continue;

                    // Index started at "--league-directory=", but we now go to the start of the directory in the string
                    indexOfInstallDirectory = commandLine.IndexOf("=", indexOfInstallDirectory) + 1;

                    // Take everything until the " behind the directory
                    return commandLine.Substring(indexOfInstallDirectory, commandLine.IndexOf("\"", indexOfInstallDirectory) - indexOfInstallDirectory);
                }
                catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
                {
                    // Intentionally empty.
                }
            }
            return null;
        }

        /**
         * Checks if the provided path is most likely a path where the LCU is installed.
         */
        private static bool IsValidLCUPath(string path)
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return false;

                string folder = Path.GetDirectoryName(path);
                return File.Exists(folder + "/LeagueClient.exe") && Directory.Exists(folder + "/Config") && Directory.Exists(folder + "/Logs");
            }
            catch
            {
                return false;
            }
        }
    }
}
