using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline
{
    public static class AuthenticationExtensions
    {
        public static async Task<string> GetExactOnlineAccessToken(this IBotContext context)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            AuthenticationResult authenticationResult;

            string authResultKey = AuthenticationConstants.AuthDialogId_ExactOnline + '_' + AuthenticationConstants.AuthResultKey;

            if (context.UserData.TryGetValue(authResultKey, out authenticationResult))
            {
                try
                {
                    var tokenCache = TokenCacheFactory.SetTokenCache(authenticationResult.TokenCache);

                    var result = await ExactOnlineHelper.GetToken(authenticationResult.UserUniqueId);
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

        public static void StoreAuthResult(this IBotContext context, AuthenticationResult authResult)
        {
            context.UserData.SetValue(AuthenticationConstants.AuthDialogId_ExactOnline + '_' + AuthenticationConstants.AuthResultKey, authResult);
        }

        public static async Task Logout(this IBotContext context, AuthenticationSettings authenticationSettings)
        {
            context.UserData.RemoveValue(AuthenticationConstants.AuthDialogId_ExactOnline + '_' + AuthenticationConstants.AuthResultKey);
            context.UserData.RemoveValue(AuthenticationConstants.AuthDialogId_ExactOnline + '_' + AuthenticationConstants.MagicNumberKey);
            context.UserData.RemoveValue(AuthenticationConstants.AuthDialogId_ExactOnline + '_' + AuthenticationConstants.MagicNumberValidated);
            string signoutURl = "https://start.exactonline.nl/api/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(authenticationSettings.RedirectUrl);
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
        }
    }
}
