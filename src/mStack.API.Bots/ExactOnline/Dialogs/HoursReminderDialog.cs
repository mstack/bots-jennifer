using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mStack.API.Bots.ExactOnline.HoursReminder;
using Microsoft.Bot.Builder.Internals.Fibers;

namespace mStack.API.Bots.ExactOnline.Dialogs
{
    /// <summary>
    /// This dialog will be called when an hour reminder is to be sent to the user
    /// </summary>
    [Serializable]
    public class HoursReminderDialog : IDialog<object>
    {
        private string _text;
        private readonly IHoursReminderService _engine;

        public HoursReminderDialog(string replyText, IHoursReminderService engine)
        {
            _text = replyText;
            SetField.NotNull(out this._engine, nameof(engine), engine);
        }

        async Task IDialog<object>.StartAsync(IDialogContext context)
        {
            await context.PostAsync(_text);
            context.Done<object>(null);
            //PromptDialog.Confirm(context, AfterPromptForSnoozing, "Do you want to snooze this alarm?");
        }

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
