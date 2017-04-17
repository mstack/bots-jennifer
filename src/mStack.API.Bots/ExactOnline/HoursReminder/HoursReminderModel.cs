using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using mStack.API.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public class HoursReminderModel : TableEntity
    {
        private static readonly string _partitionKey = "HourReminders";
        public static string StaticPartitionKey { get { return _partitionKey; } }

        public string ConversationReference { get; set; }
        public byte[] TokenCache { get; set; }
        public int? ContractHours { get; set; }

        public ConversationReference GetConversationReference()
        {
            if (this.ConversationReference != null && this.ConversationReference.Length > 0)
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ConversationReference>(this.ConversationReference);
            else
                return null;
        }

        public TokenCache GetTokenCache()
        {
            if (this.TokenCache != null && this.TokenCache.Length > 0)
                return SerializationUtilities.ByteArrayToObject<TokenCache>(this.TokenCache);
            else
                return null;
        }

        public HoursReminderModel()
        { }

        public HoursReminderModel(string username, ConversationReference conversation, int contractHours, TokenCache tokenCache)
        {
            this.ConversationReference = Newtonsoft.Json.JsonConvert.SerializeObject(conversation);
            this.TokenCache = tokenCache.Serialize();
            this.ContractHours = contractHours;

            this.PartitionKey = _partitionKey;
            this.RowKey = username;
        }
        
    }
}
