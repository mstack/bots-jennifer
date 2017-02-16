using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    [Serializable]
    public class SharePointSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string EndpointUrl { get; set; }
        public string TenantId { get; set; }
        public string TenantUrl { get; set; }
        public string RedirectUrl { get; set; }

        public SharePointSettings()
        {

        }

        public static SharePointSettings GetFromAppSettings()
        {
            try
            {
                return new SharePointSettings()
                {
                    ClientId = System.Environment.GetEnvironmentVariable("SP_CLIENT_ID"),
                    ClientSecret = System.Environment.GetEnvironmentVariable("SP_CLIENT_SECRET"),
                    TenantId = System.Environment.GetEnvironmentVariable("SP_TENANT_ID"),
                    TenantUrl = System.Environment.GetEnvironmentVariable("SP_TENANT_URL"),
                    RedirectUrl = System.Environment.GetEnvironmentVariable("SP_REDIRECT_URL")
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not load the app settings. Please check appsettings.json.");
            }
        }
    }
}
