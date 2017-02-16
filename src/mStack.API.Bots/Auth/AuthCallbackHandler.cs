using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace mStack.API.Bots.Auth
{
    public abstract class AuthCallbackHandler
    {
        private RNGCryptoServiceProvider _rngCsp = new RNGCryptoServiceProvider();
        int _maxWriteAttempts;

        public AuthCallbackHandler(int maxWriteAttempts)
        {
            _maxWriteAttempts = maxWriteAttempts;
        }

        internal abstract string dialogId { get; }
        protected abstract Task<Auth.AuthenticationResult> GetTokenByAuthCodeAsync(NameValueCollection parameters);

        public async Task<HttpResponseMessage> ProcessOAuthCallback(NameValueCollection parameters)
        {
            try
            {
                var queryParams = parameters["state"];

                var resumptionCookie = UrlToken.Decode<ResumptionCookie>(queryParams);
                // Create the message that is send to conversation to resume the login flow
                var message = resumptionCookie.GetMessage();

                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    AuthenticationResult authResult = null;

                    // Exchange the Auth code with Access token
                    authResult = await GetTokenByAuthCodeAsync(parameters);

                    IStateClient sc = scope.Resolve<IStateClient>();

                    //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                    //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                    //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                    int magicNumber = GenerateRandomNumber();
                    bool writeSuccessful = false;
                    uint writeAttempts = 0;
                    while (!writeSuccessful && writeAttempts++ < _maxWriteAttempts)
                    {
                        try
                        {
                            BotData userData = sc.BotState.GetUserData(message.ChannelId, message.From.Id);
                            userData.SetProperty(dialogId + '_' + AuthenticationConstants.AuthResultKey, authResult);
                            userData.SetProperty(dialogId + '_' + AuthenticationConstants.MagicNumberKey, magicNumber);
                            userData.SetProperty(dialogId + '_' + AuthenticationConstants.MagicNumberValidated, "false");
                            sc.BotState.SetUserData(message.ChannelId, message.From.Id, userData);
                            writeSuccessful = true;
                        }
                        catch (HttpOperationException)
                        {
                            writeSuccessful = false;
                        }
                    }
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    if (!writeSuccessful)
                    {
                        message.Text = String.Empty; // fail the login process if we can't write UserData
                        await Conversation.ResumeAsync(resumptionCookie, message);
                        resp.Content = new StringContent("<html><body>Could not log you in at this time, please try again later</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    }
                    else
                    {
                        await Conversation.ResumeAsync(resumptionCookie, message);
                        resp.Content = new StringContent($"<html><body>Almost done! Please copy this number and paste it back to your chat so your authentication can complete:<br/> <h1>{magicNumber}</h1>.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    }
                    return resp;
                }
            }
            catch (Exception ex)
            {
                // Callback is called with no pending message as a result the login flow cannot be resumed.
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return resp;
            }
        }

        private int GenerateRandomNumber()
        {
            int number = 0;
            byte[] randomNumber = new byte[1];
            do
            {
                _rngCsp.GetBytes(randomNumber);
                var digit = randomNumber[0] % 10;
                number = number * 10 + digit;
            } while (number.ToString().Length < 6);
            return number;
        }

        public static async Task<HttpResponseMessage> Resolve(HttpRequestMessage request, int maxWriteAttempts)
        {
            NameValueCollection parameters = null;

            if (request.Method == HttpMethod.Get)
            {
                parameters = request.RequestUri.ParseQueryString();
            }
            else if (request.Method == HttpMethod.Post)
            {
                parameters = await request.Content.ReadAsFormDataAsync();
            }

            // Create the message that is send to conversation to resume the login flow
            string state = parameters["state"];
            var resumptionCookie = UrlToken.Decode<ResumptionCookie>(state);
            var message = resumptionCookie.GetMessage();

            string dialogId;

            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
            {

                IStateClient sc = scope.Resolve<IStateClient>();
                BotData userData = sc.BotState.GetUserData(message.ChannelId, message.From.Id);

                dialogId = userData.GetProperty<string>(AuthenticationConstants.AuthHandlerKey);
            }

            AuthCallbackHandler handler;

            switch (dialogId)
            {
                case AuthenticationConstants.AuthDialogId_AzureAD:
                    handler = new mStack.API.Bots.AzureAD.AuthCallbackHandler(maxWriteAttempts);
                    break;
                case AuthenticationConstants.AuthDialogId_ExactOnline:
                    handler = new mStack.API.Bots.ExactOnline.AuthCallbackHandler(maxWriteAttempts);
                    break;
                default:
                    throw new ArgumentException("Unknown auth handler type.");
            }

            return await handler.ProcessOAuthCallback(parameters);
        }
    }
}
