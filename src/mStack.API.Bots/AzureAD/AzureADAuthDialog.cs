using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.Auth;

namespace mStack.API.Bots.AzureAD
{
    [Serializable]
    public class AzureADAuthDialog : Auth.AuthDialog
    {
        protected string[] scopes { get; }
        protected string resourceId { get; }

        public static string DialogId = AuthenticationConstants.AuthDialogId_AzureAD;

        public override string dialogId
        {
            get { return AzureADAuthDialog.DialogId; }
        }

        public AzureADAuthDialog(string resourceId, string prompt = "Please click to sign in: ") : base(prompt)
        {
            this.resourceId = resourceId;
        }

        public AzureADAuthDialog(string[] scopes, string prompt = "Please click to sign in: ") : base(prompt)
        {
            this.scopes = scopes;
        }

        public override Task<string> GetAccessToken(IDialogContext context)
        {
            return context.GetADALAccessToken(this.resourceId);
        }

        public override Task<string> GetAuthUrl(ResumptionCookie resumptionCookie)
        {
            return AzureADHelper.GetAuthUrlAsync(resumptionCookie, this.resourceId);
        }
    }
}
