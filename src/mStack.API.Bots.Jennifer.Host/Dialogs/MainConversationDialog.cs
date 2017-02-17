﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

//using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

using mStack.API.Bots.AzureAD;
using mStack.API.Bots.ExactOnline;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web.Configuration;
using mStack.API.Bots.ExactOnline.Dialogs;
using System.Text;
using mStack.API.Bots.SharePoint.Dialogs;
using mStack.API.Common.Utilities;
using mStack.API.REST.ExactOnlineConnect;
using mStack.API.Bots.Auth;
using mStack.API.Bots.ExactOnline.HoursReminder;

namespace mStack.API.Bots.Jennifer
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class MainConversationDialog : LuisDialog<string>
    {
        private static readonly string _resourceUriSharePoint = WebConfigurationManager.AppSettings["SP_TENANT_URL"];
        private readonly IHoursReminderService _hoursReminderService;

        public MainConversationDialog(IHoursReminderService hoursReminderService) : base(new LuisService(new LuisModelAttribute(WebConfigurationManager.AppSettings["LuisAppId"], WebConfigurationManager.AppSettings["LuisAPIKey"])))
        {
            _hoursReminderService = hoursReminderService;
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
            helptext.AppendLine(@"* Register sick leave.");

            await context.PostAsync(helptext.ToString());
            context.Wait(MessageReceived);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I didn't quite understand. Could you rephrase? You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        [LuisIntent("ClearData")]
        public async Task ClearData(IDialogContext context, LuisResult result)
        {
            context.ConversationData.Clear();
            context.UserData.Clear();
            await context.FlushAsync(CancellationToken.None);
            await context.PostAsync($"OK. I've deleted all data I had of you. It's true.");

            context.Wait(MessageReceived);
        }

        [LuisIntent("StopHoursReminder")]
        public async Task StopHoursReminder(IDialogContext context, LuisResult result)
        {
            await _hoursReminderService.RemoveReminder(context);
        }

        [LuisIntent("SetHoursReminder")]
        public async Task SetHoursReminder(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyExactOnlineAuthorization(context, activity, _resourceUriSharePoint))
            {
                await _hoursReminderService.SetReminder(context);
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("MissingHours")]
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

                double bookedHours = connector.GetBookedHours(eolConnector.EmployeeId, startDate, endDate, eolConnector);
                await context.PostAsync($"For {weekString} I found {bookedHours} hours booked.");

                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("BookHours")]
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
                    entities.Add(new EntityRecommendation(type: "ThisWeek") { Entity = "yes" });
                }
                else if (dates.Count() >= 1)
                {
                    var resolution = dates.First().Resolution;
                    Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution actual;
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse(resolution["date"], out actual))
                    {
                        // when entering "feb 1st", the parser will return -1 for the year... we will assume: this year
                        int year = actual.Year.Value != -1 ? actual.Year.Value : DateTime.Now.Year;
                        int month = actual.Month.Value != -1 ? actual.Month.Value : DateTime.Now.Month;
                        int day = actual.Day.Value != -1 ? actual.Day.Value : DateTime.Now.Day;

                        DateTime startTime = new DateTime(year, month, day);
                        entities.Add(new EntityRecommendation(type: "Date") { Entity = startTime.ToString() });
                    }
                }

                await context.PostAsync($"Hour booking... my second favorite thing to do.");

                var timeRegistrationDialog = new FormDialog<TimeRegistrationModel>(new TimeRegistrationModel(), TimeRegistrationDialog.BuildForm, FormOptions.PromptInStart, entities);
                context.Call(timeRegistrationDialog, this.ResumeAfterTimeRegistration);
            }
        }

        private async Task ResumeAfterTimeRegistration(IDialogContext context, IAwaitable<TimeRegistrationModel> result)
        {
            TimeRegistrationModel message = await result;

            await context.PostAsync("All set! Anything else I can do for you?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("LeaveRequest")]
        public async Task LeaveRequest(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyAzureADAuthorization(context, activity, _resourceUriSharePoint))
            {
                var entities = new List<EntityRecommendation>(result.Entities);

                // The next portion checks whether the discovered entities contain datetime types... if so; we 
                // will feed these as input for the formflow dialog so it doesn't have to ask the user again
                // for now this works for 1 or 2 dates and doesn't validate whether there might be more than 2

                var dates = result.Entities.Where(e => e.Type == "builtin.datetime.date");

                if (dates.Count() >= 1)
                {
                    var resolution = dates.First().Resolution;
                    Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution actual;
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse(resolution["date"], out actual))
                    {
                        // when entering "feb 1st", the parser will return -1 for the year... we will assume: this year
                        int year = actual.Year.Value != -1 ? actual.Year.Value : DateTime.Now.Year;
                        int month = actual.Month.Value != -1 ? actual.Month.Value : DateTime.Now.Month;
                        int day = actual.Day.Value != -1 ? actual.Day.Value : DateTime.Now.Day;

                        DateTime startTime = new DateTime(year, month, day);
                        entities.Add(new EntityRecommendation(type: "StartTime") { Entity = startTime.ToString() });
                    }
                }

                if (dates.Count() >= 2)
                {
                    var resolution = dates.Skip(1).First().Resolution;
                    Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution actual;
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse(resolution["date"], out actual))
                    {
                        // when entering "feb 1st", the parser will return -1 for the year... we will assume: this year
                        int year = actual.Year.Value != -1 ? actual.Year.Value : DateTime.Now.Year;
                        int month = actual.Month.Value != -1 ? actual.Month.Value : DateTime.Now.Month;
                        int day = actual.Day.Value != -1 ? actual.Day.Value : DateTime.Now.Day;

                        DateTime endTime = new DateTime(year, month, day);
                        entities.Add(new EntityRecommendation(type: "EndTime") { Entity = endTime.ToString() });
                    }
                }

                await context.PostAsync($"Holiday plans? Nice! Let's do this!");

                var leaveRequestForm = new FormDialog<LeaveRequestModel>(new LeaveRequestModel(), LeaveRequestDialog.BuildForm, FormOptions.PromptInStart, entities);
                context.Call(leaveRequestForm, this.Resume);
            }
        }

        [LuisIntent("SickLeave")]
        public async Task SickLeave(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyAzureADAuthorization(context, activity, _resourceUriSharePoint))
            {
                await context.PostAsync($"Sorry to hear about that. Let me check some things.");

                var sickLeaveForm = new FormDialog<SickLeaveModel>(new SickLeaveModel(), SickLeaveDialog.BuildForm, FormOptions.PromptInStart, result.Entities);
                context.Call(sickLeaveForm, this.Resume);
            }
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