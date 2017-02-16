using mStack.API.Bots.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth.ChannelElements;
using System.Web;

namespace mStack.API.Bots.SharePoint
{
    public class OAuthHandler : IOAuthHandler
    {
        internal static Task<string> GetUrlAsync(ResumptionCookie resumptionCookie, string resourceId)
        {
            throw new NotImplementedException();
        }

        //https://mstackbv.sharepoint.com/_vti_bin/client.svc

        static ServiceProviderDescription SharePointAuthorizationServiceDescription = new ServiceProviderDescription
        {
            RequestTokenEndpoint = new MessageReceivingEndpoint("https://accounts.accesscontrol.windows.net/49445e6c-4079-4692-8349-8bb3853f22fc/tokens/OAuth/2", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            UserAuthorizationEndpoint = new MessageReceivingEndpoint("https://mstackbv.sharepoint.com/sites/processes/_layouts/15/OAuthAuthorize.aspx", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            AccessTokenEndpoint = new MessageReceivingEndpoint("http://twitter.com/oauth/access_token", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
        };

        internal static AuthenticationResult GetTokenByAuthCodeAsync(string code, AuthenticationRequest request)
        {
            throw new NotImplementedException();
        }       

        internal static AuthenticationResult GetToken(string userUniqueId, AuthenticationRequest request)
        {
            throw new NotImplementedException();
        }

        public static async Task<AuthenticationResult> ProcessOAuthCallback(HttpRequestBase callback, AuthenticationRequest request)
        {
            IConsumerTokenManager tokenManager = OAuth.TokenCacheFactory.GetTokenCache<IConsumerTokenManager>(request);
            var signInConsumer = new WebConsumer(SharePointAuthorizationServiceDescription, tokenManager);
            var tokenResponse = signInConsumer.ProcessUserAuthorization(callback);
            signInConsumer.

            AuthenticationResult authenticationResult = new AuthenticationResult()
            {
                AccessToken = tokenResponse.AccessToken
            };

            return authenticationResult;
        }

        private static InMemoryTokenManager _tokenManager;

        private static readonly MessageReceivingEndpoint VerifyCredentialsEndpoint = new MessageReceivingEndpoint("https://mstackbv.sharepoint.com/_vti_bin/client.svc", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest);

        internal static InMemoryTokenManager GetTokenManager()
        {
            string clientId = "f485755d-217f-4d4f-a3ba-395af4d73d3e";
            string clientSecret = "";

            if (_tokenManager == null)
                _tokenManager = new InMemoryTokenManager(clientId, clientSecret);

            return _tokenManager;
        }

        private static ServiceProviderDescription GetFromAppSettings()
        {
            ServiceProviderDescription description = new ServiceProviderDescription();
            description.UserAuthorizationEndpoint = new DotNetOpenAuth.Messaging.MessageReceivingEndpoint("https://mstackbv.sharepoint.com/sites/processes/_layouts/15/OAuthAuthorize.aspx", DotNetOpenAuth.Messaging.HttpDeliveryMethods.GetRequest);
            description.AccessTokenEndpoint = new DotNetOpenAuth.Messaging.MessageReceivingEndpoint("https://accounts.accesscontrol.windows.net/49445e6c-4079-4692-8349-8bb3853f22fc/tokens/OAuth/2", DotNetOpenAuth.Messaging.HttpDeliveryMethods.PostRequest);
            description.ProtocolVersion = ProtocolVersion.V10;

            return description;
        }

        public static OutgoingWebResponse StartSignInWithTwitter(bool forceNewLogin)
        {
            var redirectParameters = new Dictionary<string, string>();
            if (forceNewLogin)
            {
                redirectParameters["force_login"] = "true";
            }
            Uri callback = MessagingUtilities.GetRequestUrlFromContext().StripQueryArgumentsWithPrefix("oauth_");
            var request = TwitterSignIn.PrepareRequestUserAuthorization(callback, null, redirectParameters);
            return TwitterSignIn.Channel.PrepareResponse(request);
        }

        public static XDocument VerifyCredentials(ConsumerBase twitter, string accessToken)
        {
            IncomingWebResponse response = twitter.PrepareAuthorizedRequestAndSend(VerifyCredentialsEndpoint, accessToken);
            return XDocument.Load(XmlReader.Create(response.GetResponseReader()));
        }
    }
}
