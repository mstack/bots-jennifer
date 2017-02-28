using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public interface IHoursReminderService
    {
        Task ProcessReminders(CancellationToken token);
        Task SetReminder(IBotContext context, int contractHours);
        Task RemoveReminder(IBotContext context);
    }
}
