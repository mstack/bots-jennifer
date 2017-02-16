using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public interface IHoursReminderStore
    {
        void AddReminder(HoursReminderModel model);
        IEnumerable<HoursReminderModel> GetReminders();
    }
}
