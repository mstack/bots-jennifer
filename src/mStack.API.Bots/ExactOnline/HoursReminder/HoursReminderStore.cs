using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    [Serializable]
    public class HoursReminderStore : IHoursReminderStore
    {
        [NonSerialized]
        CloudStorageAccount _storageAccount;
        private CloudStorageAccount StorageAccount
        {
            get
            {
                if (_storageAccount == null)
                    _storageAccount = CloudStorageAccount.Parse(WebConfigurationManager.AppSettings["AzureWebJobsStorage"]);

                return _storageAccount;
            }
        }

        readonly string _tableName = "HourReminders";

        public HoursReminderStore()
        {
            
        }

        private CloudTable GetTableClient()
        {
            var client = StorageAccount.CreateCloudTableClient();
            var table = client.GetTableReference(_tableName);
            table.CreateIfNotExists();

            return table;
        }

        /// <summary>
        /// Adds a new reminder to the reminder store
        /// </summary>
        /// <param name="model"></param>
        public void AddReminder(HoursReminderModel model)
        {
            var table = GetTableClient();
            TableOperation insertOperation = TableOperation.Insert(model);
            table.Execute(insertOperation);
        }

        public HoursReminderModel GetReminder(string username)
        {
            var table = GetTableClient();
            TableOperation retrieveOperation = TableOperation.Retrieve<HoursReminderModel>(HoursReminderModel.StaticPartitionKey, username);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            return (HoursReminderModel)retrievedResult.Result;
        }

        public IEnumerable<HoursReminderModel> GetReminders()
        {
            var table = GetTableClient();
            TableQuery<HoursReminderModel> query = new TableQuery<HoursReminderModel>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, HoursReminderModel.StaticPartitionKey));
            return table.ExecuteQuery(query);
        }

        public void DeleteReminder(string username)
        {
            var table = GetTableClient();
            TableOperation retrieveOperation = TableOperation.Retrieve<HoursReminderModel>(HoursReminderModel.StaticPartitionKey, username);
            TableResult retrievedResult = table.Execute(retrieveOperation);

            var modelToDelete = (HoursReminderModel)retrievedResult.Result;

            TableOperation deleteOperation = TableOperation.Delete(modelToDelete);
            table.Execute(deleteOperation);
        }
    }
}
