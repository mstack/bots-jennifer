using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.SharePoint
{
    public class SharePointAuthenticationRequest : OAuth.AuthenticationRequest
    {
        public SharePointAuthenticationRequest() : base(OAuth.AuthenticationRequestType.SharePointOnline)
        {

        }
    }
}
