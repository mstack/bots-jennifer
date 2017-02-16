using Microsoft.Bot.Builder.Internals.Fibers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public class HoursReminderEngine : IHoursReminderEngine
    {
        IHoursReminderStore _store;

        public HoursReminderEngine(IHoursReminderStore store)
        {
            _store = store;
        }

        public void ProcessReminders()
        {
            IEnumerable<HoursReminderModel> reminders = _store.GetReminders();
        }

        public void SetReminder(HoursReminderModel model)
        {
            _store.AddReminder(model);
        }
            
    }
}
