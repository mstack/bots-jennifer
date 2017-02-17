using Microsoft.Bot.Builder.Dialogs;
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

        public byte[] ResumptionCookie { get; set; }
        public byte[] TokenCache { get; set; }

        public ResumptionCookie GetResumptionCookie()
        {
            if (this.ResumptionCookie != null && this.ResumptionCookie.Length > 0)
                return SerializationUtilities.ByteArrayToObject<ResumptionCookie>(this.ResumptionCookie);
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

        public HoursReminderModel(string username, ResumptionCookie cookie, TokenCache tokenCache)
        {
            this.ResumptionCookie = SerializationUtilities.ObjectToByteArray(cookie);
            this.TokenCache = tokenCache.Serialize();

            this.PartitionKey = _partitionKey;
            this.RowKey = username;
        }
    }
}
