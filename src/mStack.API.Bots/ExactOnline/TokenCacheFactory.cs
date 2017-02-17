using mStack.API.Bots.Auth;
using mStack.API.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline
{
    public static class TokenCacheFactory
    {
        static TokenCache _tokenCache;

        public static TokenCache SetTokenCache(byte[] tokenCache)
        {
            _tokenCache = SerializationUtilities.ByteArrayToObject<TokenCache>(tokenCache);
            return _tokenCache;
        }

        public static TokenCache GetTokenCache()
        {
            if (_tokenCache == null)
                _tokenCache = new TokenCache();

            return _tokenCache;
        }
    }
}
