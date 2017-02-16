using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.AzureAD
{
    public static class TokenCacheFactory
    {
        static Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache _adalTokenCache;
        //static Microsoft.Identity.Client.TokenCache _msalTokenCache;

        public static Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache GetADALTokenCache()
        {
            if (_adalTokenCache == null)
                _adalTokenCache = new Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache();

            return _adalTokenCache;
        }

        internal static Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache SetADALTokenCache(byte[] tokenCache)
        {
            _adalTokenCache = new ADALTokenCache(tokenCache);
            return _adalTokenCache;
        }

        //internal static Microsoft.Identity.Client.TokenCache SetMSALTokenCache(byte[] tokenCache)
        //{
        //    _msalTokenCache = new MSALTokenCache(tokenCache);
        //    return _msalTokenCache;
        //}

        //public static Microsoft.Identity.Client.TokenCache GetMSALTokenCache()
        //{
        //    if (_msalTokenCache == null)
        //        _msalTokenCache = new Microsoft.Identity.Client.TokenCache();

        //    return _msalTokenCache;
        //}
    }
}
