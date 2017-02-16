using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public class OAuthHandler
    {
        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly uint MaxWriteAttempts = 5;

        public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
        {
            return await OAuthHandler.HandleOAuthCallback(req, MaxWriteAttempts);
        }

        public static async Task<object> HandleOAuthCallback(HttpRequestMessage req, uint maxWriteAttempts)
        {
            try
            {
                var queryParams = req.RequestUri.ParseQueryString();

                if (req.Method != HttpMethod.Post)
                    throw new ArgumentException("The OAuth postback handler only supports POST requests.");

                var formData = await req.Content.ReadAsFormDataAsync();
                string stateStr = formData["state"];
                string code = formData["code"];

                var resumptionCookie = UrlToken.Decode<ResumptionCookie>(stateStr);
                var message = resumptionCookie.GetMessage();

                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    AuthenticationResult authenticationResult = await AuthenticationHandlerFactory.GetTokenByAuthCodeAsync(code);

                    IStateClient sc = scope.Resolve<IStateClient>();

                    //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                    //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                    //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                    int magicNumber = GenerateRandomNumber();
                    bool writeSuccessful = false;
                    uint writeAttempts = 0;
                    while (!writeSuccessful && writeAttempts++ < maxWriteAttempts)
                    {
                        try
                        {
                            BotData userData = sc.BotState.GetUserData(message.ChannelId, message.From.Id);
                            userData.SetProperty(AuthenticationConstants.AuthResultKey, authenticationResult);
                            userData.SetProperty(AuthenticationConstants.MagicNumberKey, magicNumber);
                            userData.SetProperty(AuthenticationConstants.MagicNumberValidated, "false");
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
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        private static int GenerateRandomNumber()
        {
            int number = 0;
            byte[] randomNumber = new byte[1];
            do
            {
                rngCsp.GetBytes(randomNumber);
                var digit = randomNumber[0] % 10;
                number = number * 10 + digit;
            } while (number.ToString().Length < 6);
            return number;

        }
    }
}
