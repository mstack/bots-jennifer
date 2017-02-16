using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Specialized;
using mStack.API.Bots.Auth;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using RestSharp;
using Newtonsoft.Json.Linq;
using RestSharp.Authenticators;
using mStack.API.REST.ExactOnlineConnect;
using System.Web.Configuration;

namespace mStack.API.Bots.ExactOnline
{
    public class ExactOnlineHelper
    {
        private static ExactOnlineClient _client;

        public static async Task<AuthenticationResult> GetToken(string uniqueUserId)
        {
            // TODO: need to ensure that the unique user id part is included in the token cache

            var tokenCache = TokenCacheFactory.GetTokenCache();
            OAuthToken token = tokenCache.GetToken();

            // if there's a token but it's expired; we need to use the refresh token to get a new one
            if (token?.Expired() == true)
            {
                ExactOnlineClient client = GetClient();
                token.AccessToken = client.GetCurrentToken(token.RefreshToken);
                token.ExpiresOnUtcTicks = client.ExpiresAt.Ticks;
                tokenCache.CacheToken(token);
            }

            AuthenticationResult authResult = ConvertAuthenticationResult(token, tokenCache);
            return await Task.FromResult(authResult);
        }

        public static ExactOnlineConnector GetConnector()
        {
            var tokenCache = TokenCacheFactory.GetTokenCache();
            if (tokenCache == null)
                throw new ArgumentException("Cannot create Connector instance when the tokencache is null.");

            OAuthToken token = tokenCache.GetToken();
            if (token == null || String.IsNullOrEmpty(token.UserUniqueId))
                throw new ArgumentException("Cannot create Connector instance when there's no cached token or the unique user id is empty. Set a cached token.");

            ExactOnlineConnector connector = new ExactOnlineConnector(token.AccessToken);
            return connector;
        }

        public static Task<Auth.AuthenticationResult> GetTokenByAuthCode(NameValueCollection callbackParams)
        {
            var client = GetClient();
            string token = client.GetToken(callbackParams);
            var userInfo = GetUserInfo(token);

            OAuthToken oauthToken = new OAuthToken()
            {
                AccessToken = token,
                RefreshToken = client.RefreshToken,
                ExpiresOnUtcTicks = client.ExpiresAt.Ticks,
                UserName = userInfo.FullName,
                UserUniqueId = userInfo.UserID
            };

            var tokenCache = TokenCacheFactory.GetTokenCache();
            tokenCache.CacheToken(oauthToken);
            var result = ConvertAuthenticationResult(oauthToken, tokenCache);

            return Task.FromResult<Auth.AuthenticationResult>(result);
        }

        public static async Task<UserInfoModel> GetUserInfo(string uniqueUserId, string accessToken = "")
        {
            if (String.IsNullOrEmpty(accessToken))
                accessToken = (await GetToken(uniqueUserId)).AccessToken;

            AuthenticationResult authResult = await GetToken(uniqueUserId);
            return GetUserInfo(authResult.AccessToken);
        }

        private static UserInfoModel GetUserInfo(string accessToken)
        {
            var client = GetClient();
            RequestFactory factory = new RequestFactory();

            OAuth2.Client.Endpoint baseEndpoint = new OAuth2.Client.Endpoint()
            {
                BaseUri = "https://start.exactonline.nl",
            };

            OAuth2.Client.Endpoint endpoint = new OAuth2.Client.Endpoint()
            {
                BaseUri = "https://start.exactonline.nl",
                Resource = "/api/v1/current/Me"
            };

            var restClient = factory.CreateClient(baseEndpoint);
            restClient.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, "Bearer");
            IRestRequest request = factory.CreateRequest(endpoint);

            IRestResponse response = restClient.ExecuteAndVerify(request);
            return JObject.Parse(response.Content)["d"]["results"][0].ToObject<UserInfoModel>();
        }

        public static ExactOnlineClient GetClient()
        {
            if (_client == null)
            {
                var clientConfig = new RuntimeClientConfiguration()
                {
                    ClientId = WebConfigurationManager.AppSettings["EOL_CLIENT_ID"],
                    ClientSecret = WebConfigurationManager.AppSettings["EOL_CLIENT_SECRET"],
                    RedirectUri = WebConfigurationManager.AppSettings["EOL_REDIRECT_URL"],
                };

                _client = new ExactOnlineClient(new RequestFactory(), clientConfig);
            }

            return _client;
        }

        internal static AuthenticationResult ConvertAuthenticationResult(OAuthToken token, TokenCache tokenCache)
        {
            AuthenticationResult result = new AuthenticationResult()
            {
                AccessToken = token.AccessToken,
                TokenCache = tokenCache.Serialize(),
                ExpiresOnUtcTicks = token.ExpiresOnUtcTicks ?? 0,         
                UserName = token.UserName,// TODO: need to get the information for these and set 'm. user info endpoint?
                UserUniqueId = token.UserUniqueId
            };

            return result;
        }

        public static Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, string resourceId)
        {
            // https://start.exactonline.nl/api/oauth2/auth?client_id=01b85808-0248-47a8-9f25-08acd900f788&redirect_uri=http://www.mstack.nl&response_type=code&force_login=0&state=test

            var settings = AuthenticationSettings.GetFromAppSettings();

            string stateParameter = AuthUtilities.EncodeResumptionCookie(resumptionCookie);

            NameValueCollection queryParams = new NameValueCollection();
            queryParams.Add("client_id", settings.ClientId);
            queryParams.Add("redirect_uri", settings.RedirectUrl);
            queryParams.Add("response_type", "code");
            queryParams.Add("force_login", "0");
            queryParams.Add("state", stateParameter);

            string queryString = AuthUtilities.ToQueryString(queryParams);
            string result = "https://start.exactonline.nl/api/oauth2/auth" + queryString;

            return Task.FromResult(result);
        }
    }
}
