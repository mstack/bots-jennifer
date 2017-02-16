using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace mStack.API.Bots.SharePoint
{
    [Serializable]
    public class SharePointAuthDialog : Auth.AuthDialog
    {
        string siteRelativeUrl { get; set; }
        string authScope { get; set; }

        public static readonly string DialogId = "SharePoint";

        internal override string dialogId
        {
            get { return DialogId; }
        }

        public SharePointAuthDialog(string siteRelativeUrl, string authScope) : base()
        {
            this.siteRelativeUrl = siteRelativeUrl;
            this.authScope = authScope;
        }
        
        internal override Task<string> GetAccessToken(IDialogContext context)
        {
            return Task.FromResult<string>("");
            //return SharePointHelper.GetRefreshToken(context);
        }

        internal override Task<string> GetAuthUrl(ResumptionCookie resumptionCookie)
        {
            return SharePointHelper.GetAuthUrlAsync(resumptionCookie, this.siteRelativeUrl, this.authScope);
        }
    }
}
