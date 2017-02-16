using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using System.Diagnostics;
using mStack.API.Bots.Auth;

namespace mStack.API.Bots.AzureAD
{
    public static class AuthenticationExtensions
    {
        public static async Task<string> GetADALAccessToken(this IBotContext context, string resource)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            AuthenticationResult authenticationResult;

            string authenticationKey = AuthenticationConstants.AuthDialogId_AzureAD + '_' + AuthenticationConstants.AuthResultKey;

            if (context.UserData.TryGetValue(authenticationKey, out authenticationResult))
            {
                try
                {
                    var tokenCache = TokenCacheFactory.SetADALTokenCache(authenticationResult.TokenCache);
                    
                    var result = await AzureADHelper.GetToken(authenticationResult.UserUniqueId, authenticationSettings, resource);
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
        //public static async Task<string> GetMSALAccessToken(this IBotContext context, AuthenticationSettings authenticationSettings)
        //{
        //    AuthenticationResult authenticationResult;
        //    string validated = null;
        //    if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authenticationResult) &&
        //        context.UserData.TryGetValue(AuthenticationConstants.MagicNumberValidated, out validated) &&
        //        validated == "true")
        //    {

        //        try
        //        {
        //            var tokenCache = TokenCacheFactory.SetMSALTokenCache(authenticationResult.TokenCache);

        //            var result = await AzureActiveDirectoryHelper.GetToken(authenticationResult.UserUniqueId, authenticationSettings, authenticationSettings.Scopes);
        //            authenticationResult.AccessToken = result.AccessToken;
        //            authenticationResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
        //            authenticationResult.TokenCache = tokenCache.Serialize();
        //            context.StoreAuthResult(authenticationResult);
        //        }
        //        catch (Exception ex)
        //        {
        //            Trace.TraceError("Failed to renew token: " + ex.Message);
        //            await context.PostAsync("Your credentials expired and could not be renewed automatically!");
        //            await context.Logout(authenticationSettings);
        //            return null;
        //        }
        //        return authenticationResult.AccessToken;
        //    }

        //    return null;
        //}

        public static void StoreAuthResult(this IBotContext context, AuthenticationResult authResult)
        {
            context.UserData.SetValue(AzureADAuthDialog.DialogId + '_' + AuthenticationConstants.AuthResultKey, authResult);
        }

        public static async Task Logout(this IBotContext context, AuthenticationSettings authenticationSettings)
        {
            context.UserData.RemoveValue(AzureADAuthDialog.DialogId + '_' + AuthenticationConstants.AuthResultKey);
            context.UserData.RemoveValue(AzureADAuthDialog.DialogId + '_' + AuthenticationConstants.MagicNumberKey);
            context.UserData.RemoveValue(AzureADAuthDialog.DialogId + '_' + AuthenticationConstants.MagicNumberValidated);
            string signoutURl = "https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(authenticationSettings.RedirectUrl);
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
        }
    }
}
