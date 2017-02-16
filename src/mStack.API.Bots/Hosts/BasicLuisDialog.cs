
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

using mStack.API.Bots.AzureAD;
using mStack.API.Common.SharePoint;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;

using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Host;

namespace mStack.API.Bots.Hosts
{
    class BasicLuisDialog : LuisDialog<object>
    {
        [NonSerialized()]
        TraceWriter _log;

        [NonSerialized()]
        AutoResetEvent authenticationWaitHandle = new AutoResetEvent(false);

        public BasicLuisDialog(TraceWriter log) : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
        {
            this._log = log;
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I didn't quite understand. Could you rephrase? You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "MyIntent" with the name of your newly created intent in the following handler
        [LuisIntent("MyIntent")]
        public async Task MyIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached the MyIntent intent. You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        [LuisIntent("Welcome")]
        public async Task Welcome(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Welcome!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("BookHours")]
        public async Task BookHours(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Book hours.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("LeaveRequest")]
        public async Task LeaveRequest(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyAuthorization(context, activity))
            {
                var leaveRequestQuery = new LeaveRequestQuery();

                var leaveRequestForm = new FormDialog<LeaveRequestQuery>(leaveRequestQuery, LeaveRequestQuery.BuildForm, FormOptions.PromptInStart, result.Entities);
                context.Call(leaveRequestForm, this.Resume);
            }
        }


        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        private async Task<bool> VerifyAuthorization(IDialogContext context, IAwaitable<IMessageActivity> item, string resource)
        {
            var message = await item;
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            var token = await context.GetADALAccessToken(resource);
            if (string.IsNullOrEmpty(token))
            {
                await context.PostAsync($"For that action I first need to authenticate you. Please use the card to login and then try again, thanks!");
                await context.Forward(new AzureAuthDialog(authenticationSettings, resource), this.ResumeAfterAuth, message, CancellationToken.None);
                return false;
            }
            else
            {
                return true;
            }
        }


        private async Task Resume(IDialogContext context, IAwaitable<LeaveRequestQuery> result)
        {
            var request = await result;

            var message = "Got it! I'll save this to SharePoint, just a sec...";
            await context.PostAsync(message);

            var attributes = new Attribute[]
            {
            new QueueAttribute("leaverequests-items"),
            new StorageAccountAttribute("mstackfunctionapps_STORAGE")
            };

            LeaveRequest requestObj = new LeaveRequest()
            {
                Title = request.Title,
                EndTime = request.EndTime,
                StartTime = request.StartTime
            };

            ICollector<string> collector = await BinderHelper.Binder.BindAsync<ICollector<string>>(attributes);
            string json = JsonConvert.SerializeObject(requestObj);
            collector.Add(json);

            message = "Done! I've saved your request in SharePoint. Anything else I can do for you?";
            await context.PostAsync(message);

            context.Wait(MessageReceived);
        }
    }
}
