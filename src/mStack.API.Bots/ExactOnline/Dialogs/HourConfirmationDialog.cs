using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.Dialogs
{
    [Serializable]
    public class HourConfirmationDialog
    {
        public int TotalHours { get; set; }
    }
}
