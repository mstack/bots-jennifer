using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LuisBot.Controllers
{
    public class OAuthController : ApiController
    {
        private static readonly int MaxWriteAttempts = 5;

        public async Task<object> Post(HttpRequestMessage req)
        {
            return await mStack.API.Bots.Auth.AuthCallbackHandler.Resolve(req, MaxWriteAttempts);
        }


        public async Task<object> Get(HttpRequestMessage req)
        {
            return await mStack.API.Bots.Auth.AuthCallbackHandler.Resolve(req, MaxWriteAttempts);
        }
    }
}