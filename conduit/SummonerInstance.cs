using System;
using System.Windows.Forms;

namespace VoliBot
{
    public class SummonerInstance
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        
        private LeagueMonitor leagueMonitor;
        private LeagueSocketBehavior leagueSocket;

        private bool canQueue = true;
        private bool inQueue = false;
        private bool inChampSelect = false;

        /** Save calls **/
        private RiotAPI.lol_login.v1.Session curSession;

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
           //Console.WriteLine(uri);
            var data = (JsonObject)payload["data"];
            var eventType = (string)payload["eventType"];

            var status = eventType.Equals("Create") || eventType.Equals("Update") ? 200 : 404;
            var message = "[1, \"" + uri + "\", " + status + ", " + SimpleJson.SerializeObject(data) + "]";

            /** TODO: we definitly need a better way... **/
            if(eventType == "Delete") return;
            switch (uri)
            {
                /** honor popped up. **/
                case "/lol-honor-v2/v1/ballot":
                    //Console.WriteLine(payload);
                    if (data == null) return;
                    var eligiblePlayers = (JsonArray)data["eligiblePlayers"];
                    
                    int index = new Random().Next(eligiblePlayers.Count);
                    var randomPlayer = (JsonObject)eligiblePlayers[index];

                    /** we honor a random player. lucky bastard ( ͡° ͜ʖ ͡°) **/
                    long gameId = Convert.ToInt64(data["gameId"]);
                    string honorCategory = "HEART";
                    long summonerId = Convert.ToInt64(randomPlayer["summonerId"]);

                    updateStatus("We are honoring: " + randomPlayer["skinName"]);
                    leagueSocket.makeRequest("/lol-honor-v2/v1/honor-player", "POST", "{ \"gameId\": " + gameId + ", \"honorCategory\": \""+ honorCategory +"\", \"summonerId\": "+ summonerId + "}");
                    updateStatus("Game ended. Creating again an ARAM Lobby.");
                    leagueSocket.makeRequest("/lol-lobby/v2/lobby/", "POST", "{\"customGameLobby\":{\"configuration\":{\"gameMode\":\"ARAM\",\"gameMutator\":\"\",\"gameServerRegion\":\"EUW\",\"gameTypeConfig\":{},\"mapId\":12,\"maxPlayerCount\":5,\"mutators\":{},\"spectatorPolicy\":\"NotAllowed\",\"teamSize\":5,\"tournamentGameMode\":\"string\",\"tournamentPassbackDataPacket\":\"string\",\"tournamentPassbackUrl\":\"string\"},\"gameId\":11,\"lobbyName\":\"\",\"lobbyPassword\":\"\",\"spectators\":[],\"teamOne\":[],\"teamTwo\":[]},\"isCustom\":false,\"queueId\":65}");
                    break;
                case "/lol-champ-select-legacy/v1/session":
                    inQueue = false;
                    if (!inChampSelect)
                    {
                        updateStatus("In Champion Selection.");
                        inChampSelect = true;
                    }
                    break;
                case "/lol-matchmaking/v1/search":
                    if (!inQueue)
                    {
                        inQueue = true;
                        inChampSelect = false;
                        updateStatus("In Queue (Estimated Queue Time: " + Math.Round((double)data["estimatedQueueTime"], 0) + "s)");
                    }
                    break;
                case "/data-store/v1/install-settings/gameflow-process-info":
                    inChampSelect = false;
                    updateStatus("Starting League of Legends...");
                    break;
                /** auto accept any incoming ready-check **/
                case "/lol-matchmaking/v1/ready-check":
                    leagueSocket.makeRequest("/lol-matchmaking/v1/ready-check/accept", "POST");
                    break;

                /** observe login request state **/
                case "/lol-login/v1/session":
                    if (data == null) return;
                    RiotAPI.lol_login.v1.Session session = SimpleJson.DeserializeObject<RiotAPI.lol_login.v1.Session>(data.ToString());
                    switch (session.state)
                    {
                        case "SUCCEEDED":
                            updateStatus("Successfully logged in. Waiting for full initialisation.");
                            break;
                        case "LOGGING_OUT":
                            updateStatus("Logging out");
                            leagueSocket.Destroy();
                            break;
                        case "ERROR":
                            updateStatus("Credentials are wrong, or another error occured.");
                            break;
                        case "IN_PROGRESS":
                            if(session.queueStatus != null)
                                updateStatus("In Login Queue Position: " + session.queueStatus.estimatedPositionInQueue + " (" + session.queueStatus.approximateWaitTimeSeconds + ")");
                            break;
                        default:
                            updateStatus("Unknown Login Status: " + data["state"]);
                            break;
                    }
                    curSession = session;
                    break;

                /** Trying to join the matchmaking queue with the current party. **/
                case "/lol-lobby/v2/lobby":
                    /** can we start? **/
                    if (data == null) return;
                    if ((bool)data["canStartActivity"] == true)
                    {
                        canQueue = false;
                        updateStatus("Can start activity, starting queue.");
                        leagueSocket.makeRequest("/lol-matchmaking/v1/search", "POST");
                    }
                    else
                        updateStatus("Failed to initialize lobby: " + (string)data["state"]);
                    break;

                /** Waiting for full initialization. This incoming is unique when logged in. **/
                case "/lol-gameflow/v1/availability":
                    if ((bool)data["isAvailable"] && canQueue)
                    {
                        updateStatus("Creating now an ARAM Lobby.");
                        leagueSocket.makeRequest("/lol-lobby/v2/lobby/", "POST", "{\"customGameLobby\":{\"configuration\":{\"gameMode\":\"ARAM\",\"gameMutator\":\"\",\"gameServerRegion\":\"EUW\",\"gameTypeConfig\":{},\"mapId\":12,\"maxPlayerCount\":5,\"mutators\":{},\"spectatorPolicy\":\"NotAllowed\",\"teamSize\":5,\"tournamentGameMode\":\"string\",\"tournamentPassbackDataPacket\":\"string\",\"tournamentPassbackUrl\":\"string\"},\"gameId\":11,\"lobbyName\":\"\",\"lobbyPassword\":\"\",\"spectators\":[],\"teamOne\":[],\"teamTwo\":[]},\"isCustom\":false,\"queueId\":65}");
                    }
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
            leagueSocket = new LeagueSocketBehavior(port, password, this);
        }

        private void onLeagueStop()
        {
            updateStatus("League Stopped.");
            updateStatus("Disconnected from League.");
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
