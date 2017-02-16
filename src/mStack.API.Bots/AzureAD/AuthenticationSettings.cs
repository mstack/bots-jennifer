using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace mStack.API.Bots.AzureAD
{
    [Serializable]
    public class AuthenticationSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string EndpointUrl { get; set; }
        public string Tenant { get; set; }
        public string RedirectUrl { get; set; }
        public AuthenticationMode Mode { get; set; }

        public AuthenticationSettings()
        {
            
        }
        
        public static AuthenticationSettings GetFromAppSettings()
        {
            try
            {
                return new AuthenticationSettings()
                {
                    ClientId = WebConfigurationManager.AppSettings["AD_CLIENT_ID"],
                    ClientSecret = WebConfigurationManager.AppSettings["AD_CLIENT_SECRET"],
                    EndpointUrl = WebConfigurationManager.AppSettings["AD_ENDPOINT_URL"],
                    Mode = (AuthenticationMode)Enum.Parse(typeof(AuthenticationMode), WebConfigurationManager.AppSettings["AD_MODE"], true),
                    RedirectUrl = WebConfigurationManager.AppSettings["AD_REDIRECT_URL"],
                    //Resource = WebConfigurationManager.AppSettings["AD_RESOURCE"),
                    //Scopes = WebConfigurationManager.AppSettings["AD_SCOPES")?.Split(','),
                    Tenant = WebConfigurationManager.AppSettings["AD_TENANT"]
                };
            } catch (Exception ex)
            {
                throw new ArgumentException("Could not load the app settings. Please check appsettings.json.");
            }
        }
    }

    public enum AuthenticationMode
    {
        V1,
        V2,
        B2C
    }
}
