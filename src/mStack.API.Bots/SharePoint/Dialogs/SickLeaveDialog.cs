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
    public class SickLeaveModel
    {
        [Prompt("What was the start date?")]
        public DateTime StartTime { get; set; }
        [Prompt("Are you still sick at the moment?")]
        public bool StillSick { get; set; }
        [Prompt("What was the last date in your sick leave period?")]
        public DateTime EndTime { get; set; }
        [Optional]
        [Prompt("You can provide a description if you want to.")]
        public string Description { get; set; }
    }

    public static class SickLeaveDialog
    {
        static string _resourceUriSharePoint = WebConfigurationManager.AppSettings["SP_TENANT_URL"];

        public static IForm<SickLeaveModel> BuildForm()
        {
            var askEndDate = new ActiveDelegate<SickLeaveModel>((model) => { return model.StillSick == false; });

            return new FormBuilder<SickLeaveModel>()
                .Field(nameof(SickLeaveModel.StartTime))
                .Field(nameof(SickLeaveModel.StillSick))
                .Field(nameof(SickLeaveModel.EndTime), askEndDate)
                .AddRemainingFields()
                .OnCompletion(SickLeaveDialogCompleted)
                .Build();
        }

        private static async Task<SickLeaveModel> SickLeaveDialogCompleted(IBotContext context, SickLeaveModel model)
        {
            LeaveRequest request = new LeaveRequest()
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Title = $"Leave Request"
            };

            string token = await context.GetADALAccessToken(_resourceUriSharePoint);
            await SharePointConnector.CreateLeaveRequest(request, token, SharePointSettings.GetFromEnvironment());

            var message = "Done! I've saved your sick leave in SharePoint.";

            if (model.StillSick)
                message += " Get well soon!";

            await context.PostAsync(message);

            return model;
        }
    }
}
