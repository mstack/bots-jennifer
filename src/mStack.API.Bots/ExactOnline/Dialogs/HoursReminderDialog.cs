﻿using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mStack.API.Bots.ExactOnline.HoursReminder;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.FormFlow;

namespace mStack.API.Bots.ExactOnline.Dialogs
{
    [Serializable]
    public class HoursReminderDialogModel
    {
        [Prompt("How many hours do you work each week?  {||}")]
        [Describe("The amount of hours (number) on your contract.")]
        public int ContractHours { get; set; }
    }

    /// <summary>
    /// This dialog will be called when an hour reminder is to be sent to the user
    /// </summary>
    [Serializable]
    public class HoursReminderDialog : IDialog<HoursReminderDialogModel>
    {
        private readonly IHoursReminderService _engine;

        public HoursReminderDialog(IHoursReminderService engine)
        {
            SetField.NotNull(out this._engine, nameof(engine), engine);
        }

        public Task StartAsync(IDialogContext context)
        {
            throw new NotImplementedException();
        }

        public IForm<HoursReminderDialogModel> BuildForm()
        {
            return new FormBuilder<HoursReminderDialogModel>()
                .Field(nameof(HoursReminderDialogModel.ContractHours))
                .OnCompletion(SetHourReminder)
                .Build();
        }

        private async Task<HoursReminderDialogModel> SetHourReminder(IBotContext context, HoursReminderDialogModel model)
        {
            await _engine.SetReminder(context, model.ContractHours);

            return model;
        }

            //async Task IDialog<HoursReminderModel>.StartAsync(IDialogContext context)
            //{
            //    await context.PostAsync(_text);
            //    context.Done<object>(null);
            //    //PromptDialog.Confirm(context, AfterPromptForSnoozing, "Do you want to snooze this alarm?");
            //}

            //public async Task AfterPromptForSnoozing(IDialogContext context, IAwaitable<bool> snooze)
            //{
            //    try
            //    {
            //        if (await snooze)
            //        {
            //            await this.service.SnoozeAsync(this.title);
            //        }
            //        else
            //        {

            //        }
            //    }
            //    catch (TooManyAttemptsException)
            //    {
            //    }

            //    context.Done<object>(null);
            //}
        }
}
