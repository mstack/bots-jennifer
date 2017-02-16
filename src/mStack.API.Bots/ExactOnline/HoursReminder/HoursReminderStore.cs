using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public class HoursReminderStore : IHoursReminderStore
    {
        /// <summary>
        /// Adds a new reminder to the reminder store
        /// </summary>
        /// <param name="model"></param>
        public void AddReminder(HoursReminderModel model)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(WebConfigurationManager.AppSettings["StorageConnectionString"));
        }


        public IEnumerable<HoursReminderModel> GetReminders()
        {
            throw new NotImplementedException();
        }
    }
}
