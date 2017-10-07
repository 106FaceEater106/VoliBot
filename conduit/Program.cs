using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        
        private List<LeagueSocketBehavior> behaviors = new List<LeagueSocketBehavior>();
        private bool connected = false;
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

           /* Console.WriteLine("Please enter your username:");
            string username = Console.ReadLine();
            Console.WriteLine("Please enter your password:");
            string password = Console.ReadLine();
            */

            new SummonerInstance("infection0", "uni1code2", lcuPath);
            //new SummonerInstance("user2", "", lcuPath);

        }

        private string FindLocalIP()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
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
