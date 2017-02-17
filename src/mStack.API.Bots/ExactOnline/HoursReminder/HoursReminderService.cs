﻿using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using mStack.API.Bots.ExactOnline.Dialogs;
using mStack.API.Common.Utilities;
using mStack.API.REST.ExactOnlineConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    [Serializable]
    public class HoursReminderService : IHoursReminderService
    {        
        IHoursReminderStore _store;
        ResumptionCookie _cookie;

        public HoursReminderService(IHoursReminderStore store)
        {
            SetField.NotNull(out this._store, nameof(store), store);
        }

        public HoursReminderService(IHoursReminderStore store, ResumptionCookie cookie)
        {
            SetField.NotNull(out this._store, nameof(store), store);
            SetField.NotNull(out this._cookie, nameof(cookie), cookie);
        }

        public async Task ProcessReminders(CancellationToken token)
        {
            IHoursReminderStore store = new HoursReminderStore();
            IEnumerable<HoursReminderModel> reminders = store.GetReminders();

            // process all stored reminders... fetch the number of hours missing for that user and 
            // then send the user a message as a reminder
            foreach (HoursReminderModel reminder in reminders)
            {
                var tokenCache = reminder.GetTokenCache();
                var authToken = tokenCache.GetToken();

                ResumptionCookie cookie = reminder.GetResumptionCookie();

                double? bookedHours = await GetBookedHours(authToken);
                double? contractHours = 40;     // TODO: need to get the contracted hours from somewhere

                if (bookedHours == null)
                {
                    await SendReminder(cookie, "Hey! I tried to look at your hours but I was unable to. Could you be so kind to do it yourself? Thanks!", token);
                }
                else if (bookedHours == 0)
                {
                    await SendReminder(cookie, $"Hey! I noticed you didn't book any hours yet for this week. You can ask me to book your hours, or do so yourself in Exact.", token);
                }
                else if (bookedHours < contractHours)
                {
                    await SendReminder(cookie, $"Hey! I noticed you've booked {bookedHours} hours this week, I was expecting {contractHours}. Can you please book the rest?", token);
                }
            }
        }

        private async Task SendReminder(ResumptionCookie cookie, string replyText, CancellationToken token)
        {
            // use the resumption cookie to get a message to reply to
            var message = cookie.GetMessage();
            var reminderReply = message.CreateReply();
            reminderReply.Text = replyText;

            // create ConnectorClient instance with the message details and reply to the user
            var client = new ConnectorClient(new Uri(message.ServiceUrl));
            await client.Conversations.ReplyToActivityAsync(reminderReply);            
        }

        private async Task<double?> GetBookedHours(OAuthToken authToken)
        {
            try
            {
                AuthenticationResult authenticationResult = await ExactOnlineHelper.GetToken(authToken.UserUniqueId);

                ExactOnlineConnector connector = new ExactOnlineConnector(authToken.AccessToken);

                DateTime startDate, endDate;
                DateTimeUtils.GetThisWeek(DateTime.Now, out startDate, out endDate);

                TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                double bookedHours = timeConnector.GetBookedHours(connector.EmployeeId, startDate, endDate, connector);

                return bookedHours;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task SetReminder(IDialogContext context)
        {
            string username = context.Activity.From.Name;

            if (_store.GetReminder(username) != null)
            {
                await context.PostAsync("I tried to set-up a reminder for you, but it was already there!");
            }
            else
            {
                TokenCache tokenCache = TokenCacheFactory.GetTokenCache();
                HoursReminderModel model = new HoursReminderModel(username, _cookie, tokenCache);
                _store.AddReminder(model);

                await context.PostAsync("Sure thing! I will remind you about booking your hours at the end of every week.");
            }
        }
         
        public async Task RemoveReminder(IDialogContext context)
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