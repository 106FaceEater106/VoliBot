using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using WebSocketSharp.Server;

namespace MimicConduit
{
    class Program : ApplicationContext
    {
        public static string APP_NAME = "VoliBot"; // For boot identification
        public static string VERSION = "1.0.0";

        private static string Header1 = APP_NAME + " | " + VERSION;
        private static string Header2 = "based on molenzwiebel's Mimic Conduit";

        private WebSocketServer server;
        private List<LeagueSocketBehavior> behaviors = new List<LeagueSocketBehavior>();
        private NotifyIcon trayIcon;
        private bool connected = false;
        private LeagueMonitor leagueMonitor;
        private RegistryKey bootKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        private MenuItem startOnBootMenuItem;

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
            // Start the websocket server. It will not actually do anything until we add a behavior.
            server = new WebSocketServer(8182);
                
            try
            {
                server.Start();
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine($"Error code {e.ErrorCode.ToString()}: '{e.Message}'", "Unable to start server");
                return;
            }
            
            // Start monitoring league.
            leagueMonitor = new LeagueMonitor(lcuPath, onLeagueStart, onLeagueStop);
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

        private void onLeagueStart(string lockfileContents)
        {
            Console.WriteLine("League Started.");
            Console.WriteLine("Connected to League. Visit http://mimic.molenzwiebel.xyz to control your client remotely.");
            connected = true;

            var parts = lockfileContents.Split(':');
            var port = int.Parse(parts[2]);
            server.AddWebSocketService("/league", () =>
            {
                var behavior = new LeagueSocketBehavior(port, parts[3]);
                behaviors.Add(behavior);
                return behavior;
            });

            MakeDiscoveryRequest("PUT", "{ \"internal\": \"" + FindLocalIP() + "\" }");
        }

        private void onLeagueStop()
        {
            Console.WriteLine("League Stopped.");
            Console.WriteLine("Disconnected from League.");
            connected = false;

            // This will cleanup the pending connections too.
            behaviors.ForEach(x => x.Destroy());
            behaviors.Clear();
            server.RemoveWebSocketService("/league");

            MakeDiscoveryRequest("DELETE", "{}");
        }

        /// Makes an http request to the discovery server to announce or denounce our IP pairs.
        static void MakeDiscoveryRequest(string method, string body)
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                try { client.UploadString("http://discovery.mimic.molenzwiebel.xyz/discovery", method, body); } catch { }
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
