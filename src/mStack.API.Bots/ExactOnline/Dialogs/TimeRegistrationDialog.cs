using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

using mStack.API.Bots.AzureAD;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;

using Newtonsoft.Json;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using mStack.API.REST.ExactOnlineConnect;
using mStack.API.Bots.ExactOnline;
using mStack.API.Common.Utilities;

namespace mStack.API.Bots.ExactOnline.Dialogs
{

    [Serializable]
    public class TimeRegistrationModel
    {
        [Prompt("What is the customer?  {||}", ChoiceStyle = ChoiceStyleOptions.PerLine)]
        [Describe("Please provide the customer name from Exact Online.")]
        public string Customer { get; set; }

        [Prompt("Which project do you want to book on? {||}", ChoiceStyle = ChoiceStyleOptions.PerLine)]
        [Optional]
        public string Project { get; set; }

        [Prompt("Which hour type?  {||}", ChoiceStyle = ChoiceStyleOptions.PerLine)]
        [Describe("Please provide the hour type from Exact Online.")]
        public string HourType { get; set; }

        [Prompt("For this week?")]
        [Describe("When booking hours for the entire week, you can book per customer per day")]
        public bool ThisWeek { get; set; }

        [Prompt("On which date? {||}", AllowDefault = BoolDefault.False)]
        [Describe("Specify the date on which to book the hours. You can use 'today' or 'yesterday'.")]
        public DateTime Date { get; set; }

        [Prompt("Please specify your hours for each day, separated by spaces")]
        public string AmountPerDay { get; set; }

        [Prompt("Which amount? {||}", AllowDefault = BoolDefault.False)]
        [Describe("The amount of hours to book. Decimals are allowed.")]
        public double Amount { get; set; }
    }


    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class TimeRegistrationDialog
    {

        public static IForm<TimeRegistrationModel> BuildForm()
        {
            ExactOnlineConnector connector = ExactOnlineHelper.GetConnector();

            var byDate = new ActiveDelegate<TimeRegistrationModel>((state) => { return state.ThisWeek == false; });
            var askPerDay = new ActiveDelegate<TimeRegistrationModel>((state) => { return state.ThisWeek == true; });
            var verifyPerDay = new ValidateAsyncDelegate<TimeRegistrationModel>(ValidateHoursPerDay);

            return new FormBuilder<TimeRegistrationModel>()
                .Field(nameof(TimeRegistrationModel.ThisWeek))
                .Field(BuildCustomerField(connector))
                .Field(BuildProjectField(connector))
                .Field(BuildHourTypeField(connector))
                .Field(nameof(TimeRegistrationModel.Date), byDate)
                .Field(nameof(TimeRegistrationModel.Amount), byDate)
                .Field(nameof(TimeRegistrationModel.AmountPerDay), askPerDay, ValidateHoursPerDay)
                .OnCompletion(TimeRegistrationCompleted)
                .Build();
        }

        private static Task<ValidateResult> ValidateHoursPerDay(TimeRegistrationModel model, object state)
        {
            ValidateResult result = new ValidateResult();
            result.IsValid = true;

            // if the input is not a string, it's not valid in any case
            if (! (state is string))
            {
                result.Value = false;
                result.Feedback = "That doesn't seem to be a correct input, please retry. I need 5 numbers, separated by spaces.";
                return Task.FromResult(result);
            }

            string inputToValidate = (string)state;

            // split the input string by space and ensure 
            string[] hours = inputToValidate.Trim().Split(' ');
            if (!hours.All(h =>
            {
                double parseResult;
                if (double.TryParse(h, out parseResult))
                    return true;
                else
                    return false;
            }))
            {
                result.Value = false;
                result.Feedback = "Seems you entered a value that's not a number, please use only numbers.";
                Task.FromResult(result);
            }

            if (hours.Length != 5)
            {
                result.IsValid = false;
                result.Feedback = "That input was not correct. You need to specify five values, separated by spaces.";
                Task.FromResult(result);
            }

            result.Value = inputToValidate;

            return Task.FromResult(result);
        }

        private static FieldReflector<TimeRegistrationModel> BuildProjectField(ExactOnlineConnector connector)
        {
            var reflector = new FieldReflector<TimeRegistrationModel>(nameof(TimeRegistrationModel.Project));
            reflector.SetType(null);
            reflector.SetDefine((state, field) =>
            {
                Guid? customerId = !String.IsNullOrEmpty(state.Customer) ? Guid.Parse(state.Customer) : (Guid?)null;

                if (customerId != null)
                {
                    TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                    var recentProjects = timeConnector.GetRecentProjects(connector, customerId);

                    foreach (var recentProject in recentProjects)
                    {
                        field
                            .AddDescription(recentProject.ProjectId.ToString(), recentProject.ProjectDescription)
                            .AddTerms(recentProject.ProjectId.ToString(), recentProject.ProjectDescription);
                    }
                }

                return Task.FromResult(true);
            });

            return reflector;
        }

        private static FieldReflector<TimeRegistrationModel> BuildHourTypeField(ExactOnlineConnector connector)
        {
            var reflector = new FieldReflector<TimeRegistrationModel>(nameof(TimeRegistrationModel.HourType));
            reflector.SetType(null);
            reflector.SetDefine((state, field) =>
            {
                TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                var recentAccounts = timeConnector.GetRecentHourCostTypes(connector);

                foreach (var recentAccount in recentAccounts)
                {
                    field
                    .AddDescription(recentAccount.ItemId.ToString(), recentAccount.ItemDescription)
                    .AddTerms(recentAccount.ItemId.ToString(), recentAccount.ItemDescription);
                }
                return Task.FromResult(true);
            });

            return reflector;
        }

        private static FieldReflector<TimeRegistrationModel> BuildCustomerField(ExactOnlineConnector connector)
        {
            var reflector = new FieldReflector<TimeRegistrationModel>(nameof(TimeRegistrationModel.Customer));
            reflector.SetType(null);
            reflector.SetDefine((state, field) =>
            {
                TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();
                var recentAccounts = timeConnector.GetRecentAccounts(connector);

                foreach (var recentAccount in recentAccounts)
                {
                    field
                    .AddDescription(recentAccount.AccountId.ToString(), recentAccount.AccountName)
                    .AddTerms(recentAccount.AccountId.ToString(), recentAccount.AccountName);
                }
                return Task.FromResult(true);
            });

            return reflector;
        }

        private static async Task<TimeRegistrationModel> TimeRegistrationCompleted(IBotContext context, TimeRegistrationModel model)
        {
            var message = "Booking your hours in Exact, just a sec...";
            await context.PostAsync(message);

            ExactOnlineConnector connector = ExactOnlineHelper.GetConnector();

            TimeRegistrationConnector timeConnector = new TimeRegistrationConnector();

            Guid? projectId = String.IsNullOrEmpty(model.Project) || model.Project == "none" ? (Guid?)null : new Guid(model.Project);
            Guid? customerId = String.IsNullOrEmpty(model.Customer) ? (Guid?)null : new Guid(model.Customer);
            Guid? hourTypeId = String.IsNullOrEmpty(model.HourType) ? (Guid?)null : new Guid(model.HourType);

            // the user will have booked time for either this week or for a specific date 
            if (!model.ThisWeek)
            {
                timeConnector.BookHours(connector.EmployeeId, customerId, hourTypeId, projectId, model.Date, model.Amount, connector);
            }
            else
            {
                // if the hours were booked for the entire will, there will be 5 numbers in the string that need to be split 
                // out and entered for each day of the week individually
                int dayOfWeek = DateTimeUtils.GetISODayOfWeek(DateTime.Now);
                DateTime currentDay = DateTime.Now.AddDays((dayOfWeek - 1) * -1);

                string[] hours = model.AmountPerDay.Trim().Split(' ');

                for (int i = 0; i<5; i++)
                {
                    double amount = Double.Parse(hours[i]);
                    timeConnector.BookHours(connector.EmployeeId, customerId, hourTypeId, projectId, currentDay, amount, connector);
                    currentDay = currentDay.AddDays(1);
                }
            }

            return model;
         }
    }
}