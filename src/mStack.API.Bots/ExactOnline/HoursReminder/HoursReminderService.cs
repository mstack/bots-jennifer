using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using mStack.API.Bots.ExactOnline.Dialogs;
using mStack.API.Common.Utilities;
using mStack.API.REST.ExactOnlineConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    [Serializable]
    public sealed class HoursReminderService : IHoursReminderService
    {        
        private readonly IHoursReminderStore _store;

        public HoursReminderService(IHoursReminderStore store)
        {
            SetField.NotNull(out this._store, nameof(_store), store);
        }

        //public HoursReminderService(IHoursReminderStore store, ConversationReference conversation)
        //{
        //    SetField.NotNull(out this._store, nameof(_store), store);
        //    SetField.NotNull(out this._conversation, nameof(_conversation), conversation);
        //}

        //public HoursReminderService(IHoursReminderStore store, ConversationReference conversation)
        //{
        //    SetField.NotNull(out this._store, nameof(store), store);
        //    SetField.NotNull(out this._conversation, nameof(conversation), conversation);
        //}

        public async Task ProcessReminders(CancellationToken token)
        {
            IHoursReminderStore store = new HoursReminderStore();
            IEnumerable<HoursReminderModel> reminders = store.GetReminders();

            // process all stored reminders... fetch the number of hours missing for that user and 
            // then send the user a message as a reminder
            foreach (HoursReminderModel reminder in reminders)
            {
                try
                {
                    TokenCacheFactory.SetTokenCache(reminder.TokenCache);
                    var authToken = await ExactOnlineHelper.GetToken();

                    ConversationReference conversation = reminder.GetConversationReference();

                    double? bookedHours = await GetBookedHours(authToken);
                    int contractHours = reminder.ContractHours ?? 40;     // TODO: need to get the contracted hours from somewhere

                    if (bookedHours == null)
                    {
                        await SendReminder(conversation, "Hey! I tried to look at your hours but I was unable to. Could you be so kind to do it yourself? Thanks!", token);
                    }
                    else if (bookedHours == 0)
                    {
                        await SendReminder(conversation, $"Hey! I noticed you didn't book any hours yet for this week. You can ask me to book your hours, or do so yourself in Exact.", token);
                    }
                    else if (bookedHours < contractHours)
                    {
                        await SendReminder(conversation, $"Hey! I noticed you've booked {bookedHours} hours this week, I was expecting {contractHours}. Can you please book the rest?", token);
                    }
                } catch (Exception ex)
                {
                    Trace.TraceError($"Something went wrong processing the reminders: {ex}.");
                }
            }
        }

        private async Task SendReminder(ConversationReference conversation, string replyText, CancellationToken token)
        {
            var connector = new ConnectorClient(new Uri(conversation.ServiceUrl), new MicrosoftAppCredentials());

            var userAccount = new ChannelAccount(conversation.User.Id);
            var botAccount = new ChannelAccount(conversation.Bot.Id);

            Activity activity = conversation.GetPostToUserMessage();

            // need to trust the service URL because otherwise the bot connector authentication will fail
            MicrosoftAppCredentials.TrustServiceUrl(conversation.ServiceUrl);

            // construct the reply to send back to the user
            IMessageActivity messageToSend = Activity.CreateMessageActivity();
            messageToSend.ChannelId = conversation.ChannelId;
            messageToSend.From = botAccount;
            messageToSend.Recipient = userAccount;
            messageToSend.Conversation = new ConversationAccount(id: conversation.Conversation.Id);
            messageToSend.Text = replyText;
            messageToSend.Locale = "en-Us";
            messageToSend.ServiceUrl = conversation.ServiceUrl;

            await connector.Conversations.SendToConversationAsync((Activity)messageToSend);
        }

        private async Task<double?> GetBookedHours(AuthenticationResult authenticationResult)
        {
            try
            {
                ExactOnlineConnector connector = new ExactOnlineConnector(authenticationResult.AccessToken);

                DateTime startDate, endDate;
                DateTimeUtils.GetThisWeek(DateTime.Now, out startDate, out endDate);

                TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                double bookedHours = await timeConnector.GetBookedHours(connector.EmployeeId, startDate, endDate, connector);

                return bookedHours;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task SetReminder(IBotContext context, int contractHours, ConversationReference conversation)
        {
            string username = context.Activity.From.Name;

            if (_store.GetReminder(username) != null)
            {
                await context.PostAsync("I tried to set-up a reminder for you, but it was already there!");
            }
            else
            {
                TokenCache tokenCache = TokenCacheFactory.GetTokenCache();
                HoursReminderModel model = new HoursReminderModel(username, conversation, contractHours, tokenCache);
                _store.AddReminder(model);

                await context.PostAsync("Sure thing! I will remind you about booking your hours at the end of every week.");
            }
        }
         
        public async Task RemoveReminder(IBotContext context)
        {
            string username = context.Activity.From.Name;

            if (_store.GetReminder(username) == null)
            {
                await context.PostAsync("Were you actually getting reminders? I couldn't find an entry with your name.");
            }
            else
            {
                _store.DeleteReminder(username);

                await context.PostAsync("Done. Just let me know if you want this to be enabled again.");
            }
        }   
    }
}
