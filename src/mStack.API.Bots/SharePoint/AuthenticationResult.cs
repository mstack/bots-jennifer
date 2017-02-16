using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    public class AuthenticationResult: Auth.AuthenticationResult
    {
        public string Resource { get; set; }
        public string RefreshToken { get; set; }
    }
}
