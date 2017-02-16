using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using mStack.API.Bots.AzureAD;
using mStack.API.REST.SharePoint;
using mStack.API.REST.SharePoint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.SharePoint.Dialogs
{
    [Serializable]
    public class LeaveRequestModel
    {
        [Prompt("When are you leaving?")]
        public DateTime StartTime { get; set; }
        [Prompt("What's the last day of your leave period?")]
        public DateTime EndTime { get; set; }
        [Prompt("How can I name your leave request?")]
        public string Title { get; set; }
    }

    public static class LeaveRequestDialog
    {
        static string _resourceUriSharePoint = WebConfigurationManager.AppSettings["SP_TENANT_URL"];

        public static IForm<LeaveRequestModel> BuildForm()
        {
            return new FormBuilder<LeaveRequestModel>()
               .AddRemainingFields()
               .OnCompletion(LeaveRequestDialogCompleted)
               .Build();
        }

        private static async Task<LeaveRequestModel> LeaveRequestDialogCompleted(IBotContext context, LeaveRequestModel model)
        {
            LeaveRequest request = new LeaveRequest()
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Title = model.Title
            };

            string token = await context.GetADALAccessToken(_resourceUriSharePoint);
            await SharePointConnector.CreateLeaveRequest(request, token, SharePointSettings.GetFromEnvironment());

            var message = "I've saved your leave request in SharePoint! An approval request will be sent out automatically. Enjoy!";
            await context.PostAsync(message);

            return model;
        }
    }
}
