using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoliBot
{
    public class SummonerInstance
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        private bool connected = false;
        private LeagueMonitor leagueMonitor;
        private LeagueSocketBehavior leagueSocket;

        public SummonerInstance(string username, string password, string lcuPath)
        {
            Username = username;
            Password = password;

            // Start monitoring league.
            leagueMonitor = new LeagueMonitor(lcuPath, onLeagueStart, onLeagueStop);
        }

        public void volibotBehaviour(JsonObject payload)
        {
            var uri = (string)payload["uri"];
            var data = (JsonObject)payload["data"];
            var eventType = (string)payload["eventType"];

            var status = eventType.Equals("Create") || eventType.Equals("Update") ? 200 : 404;
            var message = "[1, \"" + uri + "\", " + status + ", " + SimpleJson.SerializeObject(data) + "]";

            switch (uri)
            {
                // Login, when headless client is initialized.
                case "/riotclient/system-info/v1/basic-info":
                    leagueSocket.makeRequest("/lol-login/v1/session", "POST", "{\"password\": \"" + Password + "\", \"username\": \"" + Username + "\"}");
                    break;
            }

        }

        private void onLeagueStart(int port, string password)
        {
            updateStatus("League Started.");
            updateStatus("Connected to League.");
            connected = true;
            leagueSocket = new LeagueSocketBehavior(port, password, this);
        }

        private void onLeagueStop()
        {
            updateStatus("League Stopped.");
            updateStatus("Disconnected from League.");
            connected = false;
        }
        
        public void updateStatus(string status)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[" + DateTime.Now + "] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[" + Username + "] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(status + "\n");
        }
    }
}
