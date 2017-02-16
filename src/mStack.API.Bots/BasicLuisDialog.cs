using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

using mStack.API.Bots.AzureAD;
using mStack.API.Bots.ExactOnline;
using mStack.API.Bots.ExactOnline.Dialogs;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace mStack.API.Bots
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<string>
    {
        static string _resourceUriSharePoint = Utils.GetAppSetting("SP_TENANT_URL");

        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
        {
        }

        [LuisIntent("Welcome")]
        public async Task Welcome(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Welcome!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I didn't quite understand. Could you rephrase? You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }


        [LuisIntent("BookHours")]
        public async Task BookHours(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            //if (await VerifyExactOnlineAuthorization(context, activity, ""))
            //{
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
                    entities.Add(new EntityRecommendation(type: "Date") { Entity = startTime.ToString() });
                }
            }

            var message = await activity;

            context.Call(new TimeRegistrationDialog(), this.ResumeAfterTimeRegistration);

            var timeRegistrationDialog = new FormDialog<TimeRegistrationDialog>(new TimeRegistrationDialog(), TimeRegistrationDialog.BuildForm, FormOptions.PromptInStart, entities);
            context.Call(timeRegistrationDialog, this.ResumeAfterTimeRegistration);
        }

        private async Task ResumeAfterTimeRegistration(IDialogContext context, IAwaitable<TimeRegistrationModel> result)
        {
            TimeRegistrationDialog message = await result;

            await context.PostAsync("Thanks!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Login")]
        public async Task Login(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            await context.PostAsync($"Login.");
            await context.Forward(new ExactOnlineAuthDialog(_resourceUriSharePoint), this.ResumeAfterAuth, await item, CancellationToken.None);
            //await context.Forward(new AzureADAuthDialog(_resourceUriSharePoint), this.ResumeAfterAuth, await item, CancellationToken.None);
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

                var leaveRequestForm = new FormDialog<LeaveRequestDialog>(new LeaveRequestDialog(), LeaveRequestDialog.BuildForm, FormOptions.PromptInStart, entities);
                context.Call(leaveRequestForm, this.Resume);
            }
        }

        [LuisIntent("SickLeave")]
        public async Task SickLeave(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyAzureADAuthorization(context, activity, _resourceUriSharePoint))
            {
                var sickLeaveDialog = new SickLeaveDialog();

                var sickLeaveForm = new FormDialog<SickLeaveDialog>(sickLeaveDialog, SickLeaveDialog.BuildForm, FormOptions.PromptInStart, result.Entities);
                context.Call(sickLeaveForm, this.Resume);
            }
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceived);
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


