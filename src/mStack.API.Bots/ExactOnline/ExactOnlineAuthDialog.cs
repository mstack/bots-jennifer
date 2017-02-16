using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline
{
    [Serializable]
    public class ExactOnlineAuthDialog : Auth.AuthDialog
    {
        protected string resourceId { get; }

        public static string DialogId = AuthenticationConstants.AuthDialogId_ExactOnline;

        public override string dialogId
        {
            get { return ExactOnlineAuthDialog.DialogId; }
        }

        public ExactOnlineAuthDialog(string resourceId, string prompt = "Please click to sign in: ") : base(prompt)
        {
            this.resourceId = resourceId;
        }

        public override Task<string> GetAccessToken(IDialogContext context)
        {
            return context.GetExactOnlineAccessToken();
        }

        public override Task<string> GetAuthUrl(ResumptionCookie resumptionCookie)
        {
            return ExactOnlineHelper.GetAuthUrlAsync(resumptionCookie, this.resourceId);
        }
    }
}
