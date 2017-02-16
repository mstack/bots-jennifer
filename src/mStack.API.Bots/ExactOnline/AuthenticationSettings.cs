using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.ExactOnline
{
    [Serializable]
    public class AuthenticationSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string EndpointUrl { get; set; }
        public string RedirectUrl { get; set; }

        public AuthenticationSettings()
        {

        }

        public static AuthenticationSettings GetFromAppSettings()
        {
            try
            {
                return new AuthenticationSettings()
                {
                    ClientId = WebConfigurationManager.AppSettings["EOL_CLIENT_ID"],
                    ClientSecret = WebConfigurationManager.AppSettings["EOL_CLIENT_SECRET"],
                    EndpointUrl = WebConfigurationManager.AppSettings["EOL_ENDPOINT_URL"],
                    RedirectUrl = WebConfigurationManager.AppSettings["EOL_REDIRECT_URL"],
                    //Resource = WebConfigurationManager.AppSettings["AD_RESOURCE"),
                    //Scopes = WebConfigurationManager.AppSettings["AD_SCOPES")?.Split(','),
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not load the app settings. Please check appsettings.json.");
            }
        }
    }
}
