using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    [Serializable]
    public class AuthenticationSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string EndpointUrl { get; set; }
        public string Tenant { get; set; }
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
                    ClientId = System.Environment.GetEnvironmentVariable("SP_CLIENT_ID"),
                    ClientSecret = System.Environment.GetEnvironmentVariable("SP_CLIENT_SECRET"),
                    EndpointUrl = System.Environment.GetEnvironmentVariable("SP_ENDPOINT_URL"),
                    RedirectUrl = System.Environment.GetEnvironmentVariable("SP_REDIRECT_URL"),
                    Tenant = System.Environment.GetEnvironmentVariable("SP_TENANT")
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not load the app settings. Please check appsettings.json.");
            }
        }
    }
}
