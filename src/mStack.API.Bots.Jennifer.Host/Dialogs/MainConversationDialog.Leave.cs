using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using mStack.API.Bots.SharePoint.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mStack.API.Bots.Jennifer.Dialogs
{
    public partial class MainConversationDialog
    {
        [LuisIntent("LeaveRequest.Create")]
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
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse((string)resolution["date"], out actual))
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
                    if (Microsoft.Bot.Builder.Luis.BuiltIn.DateTime.DateTimeResolution.TryParse((string)resolution["date"], out actual))
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

        [LuisIntent("SickLeave.Create")]
        public async Task SickLeave(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (await VerifyAzureADAuthorization(context, activity, _resourceUriSharePoint))
            {
                await context.PostAsync($"Sorry to hear about that. Let me check some things.");

                var sickLeaveForm = new FormDialog<SickLeaveModel>(new SickLeaveModel(), SickLeaveDialog.BuildForm, FormOptions.PromptInStart, result.Entities);
                context.Call(sickLeaveForm, this.Resume);
            }
        }
    }
}