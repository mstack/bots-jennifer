using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.Auth
{
    public static class AuthenticationConstants
    {
        public const string PersistedCookieKey = "persistedCookie";
        public const string AuthDialogIdKey = "authDialog";
        public const string AuthResultKey = "authResult";
        public const string MagicNumberKey = "authMagicNumber";
        public const string MagicNumberValidated = "authMagicNumberValidated";
        public const string AuthHandlerKey = "authHandler";
        public const string OriginalMessageText = "originalMessageText";

        public const string AuthDialogId_AzureAD = "AzureAD";
        public const string AuthDialogId_ExactOnline = "ExactOnline";
    }
}
