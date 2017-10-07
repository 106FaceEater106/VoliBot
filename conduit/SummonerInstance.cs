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
        private List<LeagueMonitor> leagueMonitoring = new List<LeagueMonitor>();

        public SummonerInstance(string username, string password, string lcuPath)
        {
            Console.WriteLine("Hello " + username);
            Username = username;
            Password = password;

            // Start monitoring league.
            LeagueMonitor leagueMonitor = new LeagueMonitor(lcuPath, onLeagueStart, onLeagueStop);
            leagueMonitoring.Add(leagueMonitor);
        }

        private void onLeagueStart(string lockfileContents)
        {
            Console.WriteLine("League Started.");
            Console.WriteLine("Connected to League. Visit http://mimic.molenzwiebel.xyz to control your client remotely.");
            connected = true;

            var parts = lockfileContents.Split(':');
            var port = int.Parse(parts[2]);
            var behavior = new LeagueSocketBehavior(port, parts[3]);
        }

        private void onLeagueStop()
        {
            Console.WriteLine("League Stopped.");
            Console.WriteLine("Disconnected from League.");
            connected = false;
        }
    }
}
