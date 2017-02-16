using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public static class OAuthExtensions
    {
        public static async Task<string> GetAccessToken(IBotContext context, AuthenticationRequest request)
        {
            AuthenticationResult authenticationResult;

            if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authenticationResult))
            {
                try
                {
                    IOAuthTokenCache tokenCache = TokenCacheFactory.SetTokenCache(request, authenticationResult.TokenCache);

                    var result = await AuthenticationHandlerFactory.GetToken(authenticationResult.UserUniqueId, authenticationSettings, resource);
                    authenticationResult.AccessToken = result.AccessToken;
                    authenticationResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                    authenticationResult.TokenCache = tokenCache.Serialize();
                    context.StoreAuthResult(authenticationResult);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to renew token: " + ex.Message);
                    await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                    await context.Logout(authenticationSettings);
                    return null;
                }
                return authenticationResult.AccessToken;
            }
            return null;
        }

        public static async Task<string> GetAccessToken(this IBotContext context, AuthenticationRequest request)
        {
            if (request is AzureAD.AzureADAuthenticationRequest)
                return await AzureAD.OAuthHelpers.GetADALAccessToken(context, (AzureAD.AzureADAuthenticationRequest)request);
            else if (request is SharePoint.SharePointAuthenticationRequest)
                return await SharePoint.OAuthHelpers.GetADALAccessToken
        }        

        public static void StoreAuthResult(this IBotContext context, AuthenticationResult authResult)
        {
            context.UserData.SetValue(AuthenticationConstants.AuthResultKey, authResult);
        }

        public static async Task Logout(this IBotContext context, AuthenticationSettings authenticationSettings)
        {
            context.UserData.RemoveValue(AuthenticationConstants.AuthResultKey);
            context.UserData.RemoveValue(AuthenticationConstants.MagicNumberKey);
            context.UserData.RemoveValue(AuthenticationConstants.MagicNumberValidated);
            string signoutURl = "https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(authenticationSettings.RedirectUrl);
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
        }
    }
}
