using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.AzureAD
{
    public static class AzureADHelper
    {
        public static async Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, string resourceId)
        {
            var extraParameters = AuthUtilities.EncodeResumptionCookie(resumptionCookie);

            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();

            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authenticationSettings.EndpointUrl + "/" + authenticationSettings.Tenant);

            Uri redirectUri = new Uri(authenticationSettings.RedirectUrl);
            var uri = await context.GetAuthorizationRequestUrlAsync(
                resourceId,
                authenticationSettings.ClientId,
                redirectUri,
                Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier.AnyUser,
                $"state={extraParameters}&response_mode=form_post");

            return uri.ToString();
        }

        //private static async Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, AuthenticationSettings authenticationSettings, string[] scopes)
        //{
        //    Uri redirectUri = new Uri(authenticationSettings.RedirectUrl);
        //    if (authenticationSettings.Mode == AuthenticationMode.V2)
        //    {
        //        MSALTokenCache tokenCache = new MSALTokenCache();
        //        Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(authenticationSettings.ClientId, redirectUri.ToString(),
        //            new Microsoft.Identity.Client.ClientCredential(authenticationSettings.ClientSecret),
        //            tokenCache);

        //        //var uri = "https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0/authorize?response_type=code" +
        //        //    "&client_id=" + AuthSettings.ClientId +
        //        //    "&client_secret=" + AuthSettings.ClientSecret +
        //        //    "&redirect_uri=" + HttpUtility.UrlEncode(AuthSettings.RedirectUrl) +
        //        //    "&scope=" + HttpUtility.UrlEncode("openid profile " + string.Join(" ", scopes)) +
        //        //    "&state=" + encodedCookie;

        //        var stateString = EncodeResumptionCookie(resumptionCookie);

        //        var uri = await client.GetAuthorizationRequestUrlAsync(
        //           scopes,
        //            null,
        //            $"state={stateString}");
        //        return uri.ToString();
        //    }
        //    else if (authenticationSettings.Mode == AuthenticationMode.B2C)
        //    {
        //        return null;
        //    }
        //    return null;
        //}

        public static async Task<AuthenticationResult> GetTokenByAuthCodeAsync(string authorizationCode)
        {
            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
            return await GetTokenByAuthCodeAsync(authorizationCode, authenticationSettings);
        }

        public static async Task<AuthenticationResult> GetTokenByAuthCodeAsync(string authorizationCode, AuthenticationSettings authenticationSettings)
        {
            var tokenCache = TokenCacheFactory.GetADALTokenCache();
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authenticationSettings.EndpointUrl + "/" + authenticationSettings.Tenant, tokenCache);
            Uri redirectUri = new Uri(authenticationSettings.RedirectUrl);
            var result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(authenticationSettings.ClientId, authenticationSettings.ClientSecret));
            AuthenticationResult authResult = ConvertAuthenticationResult(result, tokenCache);
            return authResult;
        }

        //public static async Task<AuthenticationResult> GetTokenByAuthCodeAsync(string authorizationCode, AuthenticationSettings authenticationSettings, string[] scopes)
        //{
        //    var tokenCache = TokenCacheFactory.GetMSALTokenCache();
        //    Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(authenticationSettings.ClientId, authenticationSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(authenticationSettings.ClientSecret), tokenCache);
        //    Uri redirectUri = new Uri(authenticationSettings.RedirectUrl);
        //    var result = await client.AcquireTokenByAuthorizationCodeAsync(scopes, authorizationCode);
        //    AuthenticationResult authResult = ConvertAuthenticationResult(result, tokenCache);
        //    return authResult;
        //}

        public static async Task<AuthenticationResult> GetToken(string userUniqueId, AuthenticationSettings authenticationSettings, string resourceId)
        {
            var tokenCache = TokenCacheFactory.GetADALTokenCache();
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authenticationSettings.EndpointUrl + "/" + authenticationSettings.Tenant, tokenCache);
            var result = await context.AcquireTokenSilentAsync(resourceId, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(authenticationSettings.ClientId, authenticationSettings.ClientSecret), new Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier(userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifierType.UniqueId));
            AuthenticationResult authResult = ConvertAuthenticationResult(result, tokenCache);
            return authResult;
        }

        //public static async Task<AuthenticationResult> GetToken(string userUniqueId, AuthenticationSettings authenticationSettings, string[] scopes)
        //{
        //    var tokenCache = TokenCacheFactory.GetMSALTokenCache();
        //    Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(authenticationSettings.ClientId, authenticationSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(authenticationSettings.ClientSecret), tokenCache);
        //    var result = await client.AcquireTokenSilentAsync(scopes, userUniqueId);
        //    AuthenticationResult authResult = ConvertAuthenticationResult(result, tokenCache);
        //    return authResult;
        //}

        public static AuthenticationResult ConvertAuthenticationResult(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache)
        {
            var result = new AuthenticationResult
            {
                AccessToken = authResult.AccessToken,
                UserName = $"{authResult.UserInfo.GivenName} {authResult.UserInfo.FamilyName}",
                UserUniqueId = authResult.UserInfo.UniqueId,
                ExpiresOnUtcTicks = authResult.ExpiresOn.UtcTicks,
                TokenCache = tokenCache.Serialize()
            };

            return result;
        }

        //public static AuthenticationResult ConvertAuthenticationResult(Microsoft.Identity.Client.AuthenticationResult authResult, Microsoft.Identity.Client.TokenCache tokenCache)
        //{
        //    var result = new AuthenticationResult
        //    {
        //        AccessToken = authResult.Token,
        //        UserName = authResult.User.Name,
        //        UserUniqueId = authResult.User.UniqueId,
        //        ExpiresOnUtcTicks = authResult.ExpiresOn.UtcTicks,
        //        TokenCache = tokenCache.Serialize()
        //    };

        //    return result;
        //}
    }
}
