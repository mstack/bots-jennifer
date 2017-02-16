using mStack.API.Bots.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline
{
    [Serializable]
    public class TokenCache
    {
        OAuthToken _cachedToken;

        public void CacheToken(OAuthToken token)
        {
            _cachedToken = token;
        }

        public OAuthToken GetToken()
        {
            return _cachedToken;
        }

        public byte[] Serialize()
        {
            return AuthUtilities.ObjectToByteArray(this);
        }
    }
}
