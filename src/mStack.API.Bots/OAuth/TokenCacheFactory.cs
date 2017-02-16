using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public static class TokenCacheFactory
    {
        static Dictionary<string, IOAuthTokenCache> _tokenCaches = new Dictionary<string, IOAuthTokenCache>();

        internal static IOAuthTokenCache GetTokenCache(AuthenticationRequest request)
        {
            return GetTokenCache<IOAuthTokenCache>(request);
        }

        internal static T GetTokenCache<T>(AuthenticationRequest request)
        {
            string typeName = request.GetType().FullName;

            if (_tokenCaches[typeName] == null)
            {
                if (request is AzureAD.AzureADAuthenticationRequest)
                    _tokenCaches[typeName] = new AzureAD.ADALTokenCache();
                else if (request is SharePoint.SharePointAuthenticationRequest)
                    _tokenCaches[typeName] = new SharePoint.InMemoryTokenManager();
            }

            return (T)_tokenCaches[typeName];
        }

        internal static IOAuthTokenCache SetTokenCache(AuthenticationRequest request, byte[] tokenCache)
        {
            string typeName = request.GetType().FullName;

            if (request is AzureAD.AzureADAuthenticationRequest)
                _tokenCaches[typeName] = new AzureAD.ADALTokenCache(tokenCache);
            else if (request is SharePoint.SharePointAuthenticationRequest)
                _tokenCaches[typeName] = new SharePoint.InMemoryTokenManager(tokenCache);

            return _tokenCaches[typeName];
        }

        //internal static Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache SetADALTokenCache(byte[] tokenCache)
        //{
        //    _adalTokenCache = new AzureAD.ADALTokenCache(tokenCache);
        //    return _adalTokenCache;
        //}

        //internal static Microsoft.Identity.Client.TokenCache SetMSALTokenCache(byte[] tokenCache)
        //{
        //    _msalTokenCache = new MSALTokenCache(tokenCache);
        //    return _msalTokenCache;
        //}

        //public static Microsoft.Identity.Client.TokenCache GetMSALTokenCache()
        //{
        //    if (_msalTokenCache == null)
        

        //    return _msalTokenCache;
        //}
    }
}
