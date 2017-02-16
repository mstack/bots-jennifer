using Microsoft.Bot.Builder.Dialogs;
using mStack.API.Bots.OAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.AzureAD
{
    public class OAuthHelpers
    {
        public static async Task<string> GetADALAccessToken(IBotContext context, AzureADAuthenticationRequest request)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            AuthenticationResult authenticationResult;

            if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authenticationResult))
            {
                try
                {
                    var tokenCache = TokenCacheFactory.SetADALTokenCache(authenticationResult.TokenCache);

                    var result = await AzureActiveDirectoryHelper.GetToken(authenticationResult.UserUniqueId, authenticationSettings, request.ResourceId);
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

        public static async Task<OAuth.AuthenticationResult> GetTokenByAuthCodeAsync(string authorizationCode, OAuth.AuthenticationRequest request)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            OAuth.AuthenticationResult authenticationResult;

            var tokenCache = OAuth.TokenCacheFactory.GetTokenCache<ADALTokenCache>(request);
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authenticationSettings.EndpointUrl + "/" + authenticationSettings.Tenant, tokenCache);
            Uri redirectUri = new Uri(authenticationSettings.RedirectUrl);
            var result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(authenticationSettings.ClientId, authenticationSettings.ClientSecret));
            authenticationResult = ConvertAuthenticationResult(result, tokenCache);

            return authenticationResult;
        }

        public static async Task<OAuth.AuthenticationResult> GetToken(string userUniqueId, OAuth.AuthenticationRequest request)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            OAuth.AuthenticationResult authenticationResult;

            var tokenCache = OAuth.TokenCacheFactory.GetTokenCache<ADALTokenCache>(request);
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authenticationSettings.EndpointUrl + "/" + authenticationSettings.Tenant, tokenCache);
            var result = await context.AcquireTokenSilentAsync(request.ResourceId, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(authenticationSettings.ClientId, authenticationSettings.ClientSecret), new Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier(userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifierType.UniqueId));
            authenticationResult = ConvertAuthenticationResult(result, tokenCache);

            return authenticationResult;
        }

        public static OAuth.AuthenticationResult ConvertAuthenticationResult(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache)
        {
            var result = new OAuth.AuthenticationResult
            {
                AccessToken = authResult.AccessToken,
                UserName = $"{authResult.UserInfo.GivenName} {authResult.UserInfo.FamilyName}",
                UserUniqueId = authResult.UserInfo.UniqueId,
                ExpiresOnUtcTicks = authResult.ExpiresOn.UtcTicks,
                TokenCache = tokenCache.Serialize()
            };

            return result;
        }

    }
}
