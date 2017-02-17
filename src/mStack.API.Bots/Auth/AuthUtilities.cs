using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using mStack.API.Bots.AzureAD;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace mStack.API.Bots.Auth
{
    public static class AuthUtilities
    {
        public static string EncodeResumptionCookie(ResumptionCookie resumptionCookie)
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

        public static string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(value)))
                .ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
