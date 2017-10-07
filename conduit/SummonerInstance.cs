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

        public SummonerInstance(string username, string password, string lcuPath)
        {
            Username = username;
            Password = password;

            // Start monitoring league.
            leagueMonitor = new LeagueMonitor(lcuPath, onLeagueStart, onLeagueStop);
        }

        private void onLeagueStart(int port, string password)
        {
            updateStatus("League Started.");
            updateStatus("Connected to League.");
            connected = true;
            new LeagueSocketBehavior(port, password, this);
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
