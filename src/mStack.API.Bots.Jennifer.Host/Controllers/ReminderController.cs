using mStack.API.Bots.ExactOnline.HoursReminder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace mStack.API.Bots.Jennifer.Controllers
{
    public class ReminderController : ApiController
    {
        public ReminderController()
        {

        }

        public async Task<HttpResponseMessage> Get()
        {
            HoursReminderService reminderService = new HoursReminderService(new HoursReminderStore());
            await reminderService.ProcessReminders(CancellationToken.None);

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;       
        }
    }
}
