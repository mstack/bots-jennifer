using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public static class OAuthUtilities
    {
        private static string EncodeResumptionCookie(ResumptionCookie resumptionCookie)
        {
            var encodedCookie = UrlToken.Encode(resumptionCookie);
            return encodedCookie;
        }

        public static string TokenEncoder(string token)
        {
            return WebUtility.UrlEncode(token);
        }

        public static string TokenDecoder(string token)
        {
            return WebUtility.UrlDecode(token);
        }
    }
}
