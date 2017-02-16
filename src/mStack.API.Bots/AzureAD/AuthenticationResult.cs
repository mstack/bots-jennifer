using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.AzureAD
{
    public class AuthenticationResult : Auth.AuthenticationResult
    {
        public string UserName { get; set; }
        public string UserUniqueId { get; set; }
    }
}
