using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public class HoursReminderModel
    {
        private ResumptionCookie _resumptionCookie;
        private OAuthToken _oauthToken;

        public ResumptionCookie ResumptionCookie { get { return _resumptionCookie; } }
        public OAuthToken OAuthToken { get { return _oauthToken; } }

        public HoursReminderModel()
        { }

        public HoursReminderModel(ResumptionCookie cookie, OAuthToken token)
        {
            this._resumptionCookie = cookie;
            this._oauthToken = token;
        }
    }
}
