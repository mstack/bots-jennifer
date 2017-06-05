using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using mStack.API.Bots.Dialogs;
using mStack.API.Bots.ExactOnline;
using mStack.API.Bots.ExactOnline.Dialogs;
using mStack.API.Bots.Utilities;
using mStack.API.Common.Utilities;
using mStack.API.REST.ExactOnlineConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace mStack.API.Bots.Jennifer.Dialogs
{
    public partial class MainConversationDialog : LuisDialog<string>
    {
        [LuisIntent("Hours.StopReminder")]
        public async Task StopHoursReminder(IDialogContext context, LuisResult result)
        {
            await _hoursReminderService.RemoveReminder(context);
        }

        [LuisIntent("Hours.SetReminder")]
        public async Task SetHoursReminder(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyExactOnlineAuthorization(context, activity, _resourceUriSharePoint))
            {
                //await _hoursReminderService.SetReminder(context);
                //context.Wait(MessageReceived);

                HoursReminderDialog dialog = new HoursReminderDialog(_hoursReminderService);

                var hoursReminderDialog = new FormDialog<HoursReminderDialogModel>(new HoursReminderDialogModel(), dialog.BuildForm, FormOptions.PromptInStart);
                context.Call(hoursReminderDialog, this.ResumeAfterReminder);
            }
        }

        private async Task ResumeAfterReminder(IDialogContext context, IAwaitable<HoursReminderDialogModel> result)
        {
            context.Wait(MessageReceived);
        }

        [LuisIntent("Hours.Missing")]
        public async Task MissingHours(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyExactOnlineAuthorization(context, activity, ""))
            {
                DateTime startDate;
                DateTime endDate;
                string weekString;

                var message = await activity;
                if (message.Text.ToLower().Contains("last week"))
                {
                    weekString = "last week";
                    DateTimeUtils.GetThisWeek(DateTime.Now.AddDays(-7), out startDate, out endDate);
                }
                else
                {
                    weekString = "this week";
                    DateTimeUtils.GetThisWeek(DateTime.Now, out startDate, out endDate);
                }

                await context.PostAsync($"Just a minute, going to check your hours for {weekString}...");

                ExactOnlineConnector eolConnector = ExactOnlineHelper.GetConnector();
                TimeRegistrationConnector connector = new TimeRegistrationConnector();

                double bookedHours = await connector.GetBookedHours(eolConnector.EmployeeId, startDate, endDate, eolConnector);
                await context.PostAsync($"For {weekString} I found {bookedHours} hours booked.");

                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("Hours.Book")]
        public async Task BookHours(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;

            if (await VerifyExactOnlineAuthorization(context, activity, ""))
            {
                var entities = new List<EntityRecommendation>(result.Entities);

                // The next portion checks whether the discovered entities contain datetime types... if so; we 
                // will feed these as input for the formflow dialog so it doesn't have to ask the user again
                // for now this works for 1 or 2 dates and doesn't validate whether there might be more than 2

                var dates = result.Entities.Where(e => e.Type == "builtin.datetime.date");

                if (message.Text.ToLower().Contains("this week"))
                {
                    entities.Add(new EntityRecommendation(type: nameof(TimeRegistrationModel.ThisWeek)) { Entity = "yes" });
                }
                else if (dates.Count() >= 1)
                {
                    var resolution = dates.First().Resolution;
                    Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution actual;
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse((string)resolution["date"], out actual))
                    {
                        DateTime startTime = actual.ConvertResolutionToDateTime();

                        entities.Add(new EntityRecommendation(type: nameof(TimeRegistrationModel.Date)) { Entity = startTime.ToString() });
                        entities.Add(new EntityRecommendation(type: nameof(TimeRegistrationModel.ThisWeek)) { Entity = "no" });
                    }
                }

                var numberEntities = result.Entities.Where(e => e.Type == "builtin.datetime.duration");
                if (numberEntities.Count() == 1)
                {
                    var resolution = numberEntities.First().Resolution;
                    string durationString = (string)resolution["duration"];

                    Match regexMatch = Regex.Match(durationString, "PT(\\d)H");
                    if (regexMatch.Success)
                    {
                        string amount = regexMatch.Groups[1].Value;
                        entities.Add(new EntityRecommendation(type: nameof(TimeRegistrationModel.Amount)) { Entity = amount });
                    }
                }

                await context.PostAsync($"Hour booking... my second favorite thing to do.");

                TimeRegistrationDialog dialog = new TimeRegistrationDialog(_botCache, message.From.Id);

                var timeRegistrationDialog = new FormDialog<TimeRegistrationModel>(new TimeRegistrationModel(), dialog.BuildForm, FormOptions.PromptInStart, entities);
                context.Call(timeRegistrationDialog, this.ResumeAfterTimeRegistration);
            }
        }

        [LuisIntent("Hours.Submit")]
        public async Task SubmitHours(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyExactOnlineAuthorization(context, activity, ""))
            {
                await context.PostAsync($"Let me check whether you're all set...");

                ExactOnlineConnector eolConnector = ExactOnlineHelper.GetConnector();
                TimeRegistrationConnector connector = new TimeRegistrationConnector();

                DateTime startDate, endDate;
                DateTimeUtils.GetThisWeek(DateTime.Now, out startDate, out endDate);

                double bookedHours = await connector.GetBookedHours(eolConnector.EmployeeId, startDate, endDate, eolConnector);

                ConfirmDialog.Text = $"You've registered a total number of {0} hours for this week. Do you want me to submit those?";
                var confirmationDialog = new FormDialog<ConfirmModel>(new ConfirmModel(), ConfirmDialog.BuildForm, FormOptions.PromptInStart);
                context.Call(confirmationDialog, this.ResumeAfterSubmitHoursDialog);
            }
        }

        private async Task ResumeAfterSubmitHoursDialog(IDialogContext context, IAwaitable<ConfirmModel> result)
        {
            ConfirmModel message = await result;

            if (message.Confirmation)
            {
                DateTime startDate, endDate;
                DateTimeUtils.GetThisWeek(DateTime.Now, out startDate, out endDate);

                ExactOnlineConnector eolConnector = ExactOnlineHelper.GetConnector();
                TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                timeConnector.SubmitHours(eolConnector.EmployeeId, startDate, endDate, eolConnector);

                await context.PostAsync($"Thanks, I've closed your timesheet for this week. Have a nice weekend!");
            }
            else
            {
                await context.PostAsync($"Ok. Just give me a nudge when you're ready.");
            }

            context.Wait(MessageReceived);
        }

        private async Task ResumeAfterTimeRegistration(IDialogContext context, IAwaitable<TimeRegistrationModel> result)
        {
            TimeRegistrationModel message = await result;
            context.Wait(MessageReceived);
        }
    }
}