using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mStack.API.Bots.Auth;

namespace mStack.API.Bots.SharePoint
{
    public class AuthCallbackHandler : Auth.AuthCallbackHandler
    {
        public AuthCallbackHandler(int maxWriteAttempts) : base(maxWriteAttempts)
        {
        }

        protected async override Task<Auth.AuthenticationResult> GetTokenByAuthCodeAsync(string code)
        {
            return await SharePointHelper.GetTokenByAuthCodeAsync(code);
        }
    }
}
