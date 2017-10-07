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
            try
            {
               // updateStatus(uri);
            }
            catch (Exception e)
            {

            }
            var eventType = (string)payload["eventType"];

            var status = eventType.Equals("Create") || eventType.Equals("Update") ? 200 : 404;
            var message = "[1, \"" + uri + "\", " + status + ", " + SimpleJson.SerializeObject(data) + "]";

            /** TODO: we definitly need a better way... **/
            switch (uri)
            {
                /** auto accept any incoming ready-check **/
                case "/lol-matchmaking/v1/ready-check":
                    leagueSocket.makeRequest("/lol-matchmaking/v1/ready-check/accept", "POST");
                    break;

                /** observe login request state **/
                case "/lol-login/v1/session":
                    var state = (string)data["state"];
                    if (state == "SUCCEEDED")
                    {
                        updateStatus("Successfully logged in. Waiting for full initialisation.");
                    }
                    else if (state == "ERROR")
                        updateStatus("Credentials are wrong, or another error occured.");
                    else if (state != "IN_PROGRESS")
                        updateStatus("Unknown Login Status: " + data["state"]);
                    break;

                /** Trying to join the matchmaking queue with the current party. **/
                case "/lol-lobby/v2/lobby":
                    /** can we start? **/
                    if (data == null) return;
                    if ((bool)data["canStartActivity"] == true)
                    {
                        updateStatus("Starting queue.");
                        leagueSocket.makeRequest("/lol-matchmaking/v1/search", "POST");
                    }
                    else
                        updateStatus("Failed to initialize lobby: " + (string)data["state"]);
                    break;

                /** Waiting for full initialization. This incoming is unique when logged in. **/
                case "/data-store/v1/install-settings/login-remember-me":
                    updateStatus("Creating now an ARAM Lobby.");
                    leagueSocket.makeRequest("/lol-lobby/v2/lobby/", "POST", "{\"customGameLobby\":{\"configuration\":{\"gameMode\":\"ARAM\",\"gameMutator\":\"\",\"gameServerRegion\":\"EUW\",\"gameTypeConfig\":{},\"mapId\":12,\"maxPlayerCount\":5,\"mutators\":{},\"spectatorPolicy\":\"NotAllowed\",\"teamSize\":5,\"tournamentGameMode\":\"string\",\"tournamentPassbackDataPacket\":\"string\",\"tournamentPassbackUrl\":\"string\"},\"gameId\":11,\"lobbyName\":\"\",\"lobbyPassword\":\"\",\"spectators\":[],\"teamOne\":[],\"teamTwo\":[]},\"isCustom\":false,\"queueId\":65}");
                    break;

                /** send a login request when recieving basic info **/
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
