using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace mStack.API.Bots.OAuth
{
    public static class AuthenticationHandlerFactory
    {
        internal static Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, AuthenticationRequest request)
        {
            if (request is AzureAD.AzureADAuthenticationRequest)
                return AzureAD.AzureActiveDirectoryHelper.GetAuthUrlAsync(resumptionCookie, request.ResourceId);
            else if (request is SharePoint.SharePointAuthenticationRequest)
                return SharePoint.OAuthHandler.GetUrlAsync(resumptionCookie, request.ResourceId);
            else
                throw new ArgumentOutOfRangeException("The AuthenticationRequest type of parameter 'request' is not supported.");
        }

        internal static Task ProcessOAuthCallback(HttpRequestBase request, )

        internal static async Task<AuthenticationResult> GetTokenByAuthCodeAsync(AuthenticationRequest request, string code)
        {
            if (request is AzureAD.AzureADAuthenticationRequest)
                return await AzureAD.OAuthHelpers.GetTokenByAuthCodeAsync(code, request);
            else if (request is SharePoint.SharePointAuthenticationRequest)
                return SharePoint.OAuthHandler.GetTokenByAuthCodeAsync(code, request);
            else
                throw new ArgumentOutOfRangeException("The AuthenticationRequest type of parameter 'request' is not supported.");
        }

        public static async Task<AuthenticationResult> GetToken(string userUniqueId, AuthenticationRequest request)
        {
            if (request is AzureAD.AzureADAuthenticationRequest)
                return await AzureAD.OAuthHelpers.GetToken(userUniqueId, request);
            else if (request is SharePoint.SharePointAuthenticationRequest)
                return SharePoint.OAuthHandler.GetToken(userUniqueId, request);
            else
                throw new ArgumentOutOfRangeException("The AuthenticationRequest type of parameter 'request' is not supported.");
        }
    }
}
