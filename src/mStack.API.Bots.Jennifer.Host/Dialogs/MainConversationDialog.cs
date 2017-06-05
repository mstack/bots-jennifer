using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

using mStack.API.Bots.AzureAD;
using mStack.API.Bots.ExactOnline;

using System.Web.Configuration;
using System.Text;
using mStack.API.Bots.Auth;
using mStack.API.Bots.ExactOnline.HoursReminder;
using mStack.API.Bots.Cache;

namespace mStack.API.Bots.Jennifer.Dialogs
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public partial class MainConversationDialog : LuisDialog<string>
    {
        private static readonly string _resourceUriSharePoint = WebConfigurationManager.AppSettings["SP_TENANT_URL"];
        private readonly IHoursReminderService _hoursReminderService;
        private readonly IBotCache _botCache;

        public MainConversationDialog(IHoursReminderService hoursReminderService, IBotCache botCache, LuisService luisService) : base(luisService)
        {
            SetField.NotNull(out this._hoursReminderService, nameof(_hoursReminderService), hoursReminderService);
            SetField.NotNull(out this._botCache, nameof(_botCache), botCache);
        }

        [LuisIntent("Welcome")]
        public async Task Welcome(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Hi there! What can I do for you today?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            StringBuilder helptext = new StringBuilder();
            helptext.AppendLine(@"Here's a list of things you can ask me to do:");
            helptext.AppendLine(@"* Ask me to book your holiday request and I'll save it for you.");
            helptext.AppendLine(@"* Book your hours. For today, a specific date or this week in one go.");
            helptext.AppendLine(@"* Ask me to remind you to book your hours. I'll send you a reminder at the end of each week and month.");
            helptext.AppendLine(@"* Register sick leave.");

            await context.PostAsync(helptext.ToString());
            context.Wait(MessageReceived);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I didn't quite understand. Could you rephrase? You said: {result.Query}");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Data.Clear")]
        public async Task ClearData(IDialogContext context, LuisResult result)
        {
            context.ConversationData.Clear();
            context.UserData.Clear();
            await context.FlushAsync(CancellationToken.None);
            await context.PostAsync($"OK. I've deleted all data I had of you. It's true.");

            context.Wait(MessageReceived);
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            string dialogId;
            string originalMessageText;

            await context.PostAsync(message);

            try
            {
                context.UserData.TryGetValue(AuthenticationConstants.AuthDialogIdKey, out dialogId);
                context.UserData.TryGetValue(dialogId + '_' + AuthenticationConstants.OriginalMessageText, out originalMessageText);

                IMessageActivity originalMessage = context.MakeMessage();
                originalMessage.Text = originalMessageText;
                IAwaitable<IMessageActivity> awaitableMessage = Awaitable.FromItem(originalMessage);

                await MessageReceived(context, awaitableMessage);
            }
            catch (Exception ex)
            {
                context.Wait(MessageReceived);
            }
        }

        private async Task<bool> VerifyAzureADAuthorization(IDialogContext context, IAwaitable<IMessageActivity> item, string resource)
        {
            var message = await item;
            var token = await context.GetADALAccessToken(resource);

            if (string.IsNullOrEmpty(token))
            {
                await context.PostAsync($"For that action I first need to authenticate you. Please use the card to login and then try again, thanks!");
                await context.Forward(new AzureADAuthDialog(resource), this.ResumeAfterAuth, message, CancellationToken.None);

                return false;
            }
            else
            {
                return true;
            }
        }

        private async Task<bool> VerifyExactOnlineAuthorization(IDialogContext context, IAwaitable<IMessageActivity> item, string resource)
        {
            var message = await item;
            var token = await context.GetExactOnlineAccessToken();

            if (string.IsNullOrEmpty(token))
            {
                await context.PostAsync($"For that action I first need to authenticate you. Please use the card to login and then try again, thanks!");
                await context.Forward(new ExactOnlineAuthDialog(resource), this.ResumeAfterAuth, message, CancellationToken.None);

                return false;
            }
            else
            {
                return true;
            }
        }

        private async Task Resume(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                await result;
            }
            catch (Exception ex)
            {
                await context.PostAsync("You canceled the form!");
                return;
            }

            context.Wait(MessageReceived);
        }
    }
}