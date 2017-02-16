using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    public static class AuthenticationExtensions
    {
        public static async Task<string> GetSharePointAccessToken(this IBotContext context, string resource)
        {
            SharePointSettings sharepointSettings = SharePointSettings.GetFromAppSettings();
            AuthenticationResult authenticationResult;

            if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authenticationResult))
            {
                try
                {
                    // here needs to come an implementation that automatically renews the refresh token we get from SharePoint
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to renew token: " + ex.Message);
                    await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                    await context.Logout(sharepointSettings);
                    return null;
                }

                return authenticationResult.AccessToken;
            }

            return null;
        }

        public static void StoreAuthResult(this IBotContext context, AuthenticationResult authResult)
        {
            context.UserData.SetValue(AuthenticationConstants.AuthResultKey + '_' + SharePointAuthDialog.DialogId, authResult);
        }

        public static async Task Logout(this IBotContext context, SharePointSettings sharePointSettings)
        {
            context.UserData.RemoveValue(AuthenticationConstants.AuthResultKey + '_' + SharePointAuthDialog.DialogId);
            context.UserData.RemoveValue(AuthenticationConstants.MagicNumberKey + '_' + SharePointAuthDialog.DialogId);
            context.UserData.RemoveValue(AuthenticationConstants.MagicNumberValidated + '_' + SharePointAuthDialog.DialogId);
            string signoutURl = "https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(sharePointSettings.RedirectUrl);
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
        }
    }
}
