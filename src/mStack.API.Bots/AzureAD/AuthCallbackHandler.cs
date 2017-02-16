using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace mStack.API.Bots.AzureAD
{
    public class AuthCallbackHandler : Auth.AuthCallbackHandler
    {
        public AuthCallbackHandler(int maxWriteAttempts) : base(maxWriteAttempts)
        {

        }

        internal override string dialogId => AuthenticationConstants.AuthDialogId_AzureAD;

        protected override async Task<Auth.AuthenticationResult> GetTokenByAuthCodeAsync(NameValueCollection parameters)
        {
            string code = parameters["code"];

            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            // Exchange the Auth code with Access token
            var token = await AzureADHelper.GetTokenByAuthCodeAsync(code, authenticationSettings);
            return token;
        }
    }
}
