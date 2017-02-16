using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    public class OAuthHelpers
    {
        public static async Task<string> GetSPAccessToken(IBotContext context, SharePointAuthenticationRequest request)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            AuthenticationResult authenticationResult;

            if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authenticationResult))
            {
                try
                {
                    var tokenCache = TokenCacheFactory.SetADALTokenCache(authenticationResult.TokenCache);

                    var result = await SharePointHelper.GetToken(authenticationResult.UserUniqueId, authenticationSettings, resource);
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
    }
}
