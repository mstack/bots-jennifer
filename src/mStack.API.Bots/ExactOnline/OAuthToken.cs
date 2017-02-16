using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline
{
    [Serializable]
    public class OAuthToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public long? ExpiresOnUtcTicks { get; set; }
        public string UserName { get; internal set; }
        public string UserUniqueId { get; internal set; }

        public bool Expired()
        {
            if (ExpiresOnUtcTicks != null)
            {
                // TODO: this is very dodgy, but works for now... 
                return ExpiresOnUtcTicks <= DateTime.Now.Ticks;
            }
            else
            {
                return true;
            }
        }
    }
}
