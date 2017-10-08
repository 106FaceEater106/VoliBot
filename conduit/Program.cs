using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Windows.Forms;
using WebSocketSharp.Server;

namespace VoliBot
{
    class Program : ApplicationContext
    {
        public static string APP_NAME = "VoliBot"; // For boot identification
        public static string VERSION = "1.0.0";

        private static string Header1 = APP_NAME + " | " + VERSION;
        private static string Header2 = "based on molenzwiebel's Mimic Conduit";

        private ArrayList accounts = new ArrayList();

        private List<LeagueSocketBehavior> behaviors = new List<LeagueSocketBehavior>();
        private List<LeagueMonitor> leagueMonitoring = new List<LeagueMonitor>();

        private RegistryKey bootKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private Program(string lcuPath)
        {
            Console.Title = APP_NAME + " " + VERSION;
            Console.ForegroundColor = ConsoleColor.White;
            for(int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write("=");
            }
            Console.Write("\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + Header1.Length / 2) + "}", Header1));
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + Header2.Length / 2) + "}", Header2));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n");
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write("=");
            }
            
            foreach(string account in loadAccounts())
            {
                string[] stringSeparators = new string[] { "|" };
                var result = account.Split(stringSeparators, StringSplitOptions.None);
                new SummonerInstance(result[0], result[1], lcuPath);
            }
        }

        public ArrayList loadAccounts()
        {
            ArrayList accounts = new ArrayList();
            TextReader tr = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "accounts.txt");
            string line;
            while ((line = tr.ReadLine()) != null)
            {
                accounts.Add(line);
            }
            tr.Close();
            return accounts;
        }

        [STAThread]
        static void Main()
        {
            try
            {
                using (new SingleGlobalInstance(500)) // Wait 500 seconds max for other programs to stop
                {
                    var lcuPath = LeagueMonitor.GetLCUPath();
                    if (lcuPath == null)
                    {
                        MessageBox.Show("Could not determine path to LCU!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return; // Abort
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Program(lcuPath));
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("VoliBot is already running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
