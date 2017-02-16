using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mStack.API.Bots.Auth;
using System.Web;
using System.Net.Http;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using OAuth2.Infrastructure;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using System.Collections.Specialized;

namespace mStack.API.Bots.ExactOnline
{
    public class AuthCallbackHandler : Auth.AuthCallbackHandler
    {
        public OAuth2.Client.Impl.ExactOnlineClient Client { get; set; }

        internal override string dialogId => AuthenticationConstants.AuthDialogId_ExactOnline;

        public AuthCallbackHandler(int maxWriteAttempts) : base(maxWriteAttempts)
        {
            Client = ExactOnlineHelper.GetClient();
        }

        protected override Task<Auth.AuthenticationResult> GetTokenByAuthCodeAsync(NameValueCollection parameters)
        {
            return ExactOnlineHelper.GetTokenByAuthCode(parameters);
        }
    }
}
