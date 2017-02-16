using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }
        public string UserName { get; set; }
        public string UserUniqueId { get; set; }
        public long ExpiresOnUtcTicks { get; set; }
        public byte[] TokenCache { get; set; }
    }
}