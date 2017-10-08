using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoliBot;
using System.Threading.Tasks;

namespace VoliBot.RiotAPI.lol_login.v1
{
    public class Session
    {
        public String accountId { get; set; }
        public Boolean connected { get; set; }
        public Error error { get; set; }
        public GasToken gasToken { get; set; }
        public String idToken { get; set; }
        public Boolean isNewPlayer { get; set; }
        public String puuid { get; set; }
        public QueueStatus queueStatus { get; set; }
        public String state { get; set; }
        public String summonerId { get; set; }
        public String userAuthToken { get; set; }
        public String username { get; set; }
    }
    
    public class Error
    {
        public String description { get; set; }
        public String id { get; set; }
        public String messageId { get; set; }
    }

    public class GasToken
    {
        public String date_time { get; set; }
        public String gas_account_id { get; set; }
        public String pvpnet_account_id { get; set; }
        public String signature { get; set; }
        public String vouching_key_id { get; set; }
    }

    public class QueueStatus
    {
        public Int64 approximateWaitTimeSeconds { get; set; }
        public Int64 estimatedPositionInQueue { get; set; }
        public Boolean isPositionCapped { get; set; }
    }

}
