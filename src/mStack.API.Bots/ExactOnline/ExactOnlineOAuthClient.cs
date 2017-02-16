using DotNetOpenAuth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.ExactOnline
{
    public class ExactOnlineOAuthClient : WebServerClient
    {
        #region Properties

        public IAuthorizationState Authorization { get; set; }

        #endregion

        #region Constructor

        public ExactOnlineOAuthClient()
            : base(CreateAuthorizationServerDescription(), MyClientIdentifier(), MyClientSecret())
        {
            // initialization is already done through the base constructor
            ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(MyClientSecret());
        }

        #endregion

        public static AuthorizationServerDescription CreateAuthorizationServerDescription()
        {
            var baseUri = WebConfigurationManager.AppSettings["EOL_ENDPOINT_URL"];
            var uri = new Uri(baseUri.EndsWith("/") ? baseUri : baseUri + "/");
            var serverDescription = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(uri, "api/oauth2/auth"),
                TokenEndpoint = new Uri(uri, "api/oauth2/token")
            };

            return serverDescription;
        }


        public static string MyClientIdentifier()
        {
            return WebConfigurationManager.AppSettings["EOL_CLIENT_ID"];
        }

        public static string MyClientSecret()
        {
            return WebConfigurationManager.AppSettings["EOL_CLIENT_SECRET"];
        }

        private Boolean AccessTokenHasToBeRefreshed()
        {
            TimeSpan timeToExpire = Authorization.AccessTokenExpirationUtc.Value.Subtract(DateTime.UtcNow);

            return (timeToExpire.Minutes < 1);
        }
    }
}
