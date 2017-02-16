using System;
using System.Net;
using System.Threading;

using Newtonsoft.Json;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;

using mStack.API.Bots.AzureAD;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace mStack.API.Bots.Hosts
{
    public class Host
    {

        static readonly uint MaxWriteAttempts = 5;

        public static async Task<object> Run(HttpRequestMessage req, Binder binder, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            try
            {
                var queryParams = req.RequestUri.ParseQueryString();

                string stateStr = queryParams["state"];
                string code = queryParams["code"];

                // requests with an empty body will be OAuth callbacks from Azure, handle accordingly
                if (String.IsNullOrEmpty(await req.Content.ReadAsStringAsync()) &&
                   !String.IsNullOrEmpty(stateStr) &&
                   !String.IsNullOrEmpty(code))
                    return await OAuthHandler.HandleOAuthCallback(req, MaxWriteAttempts);
            }
            catch (Exception ex)
            {
                log.Error("Fault with OAuth callback: " + ex.ToString());
            }

            AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();

            // Initialize the azure bot
            using (BotService.Initialize())
            {
                // Deserialize the incoming activity
                string jsonContent = await req.Content.ReadAsStringAsync();
                var activity = JsonConvert.DeserializeObject<Activity>(jsonContent);

                // authenticate incoming request and add activity.ServiceUrl to MicrosoftAppCredentials.TrustedHostNames
                // if request is authenticated
                if (!await BotService.Authenticator.TryAuthenticateAsync(req, new[] { activity }, CancellationToken.None))
                {
                    return BotAuthenticator.GenerateUnauthorizedResponse(req);
                }

                BinderHelper.Binder = binder;

                if (activity != null)
                {
                    // one of these will have an interface and process it
                    switch (activity.GetActivityType())
                    {
                        case ActivityTypes.Message:
                            //await Conversation.SendAsync(activity, () => new AzureAuthDialog(authenticationSettings));
                            await Conversation.SendAsync(activity, () => new BasicLuisDialog(log));
                            break;
                        case ActivityTypes.ConversationUpdate:
                            await ConversationUpdate(activity);
                            break;
                        case ActivityTypes.ContactRelationUpdate:
                        case ActivityTypes.Typing:
                        case ActivityTypes.DeleteUserData:
                        case ActivityTypes.Ping:
                        default:
                            log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                            break;
                    }
                }
                return req.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        public static async Task ConversationUpdate(Activity activity)
        {
            var client = new ConnectorClient(new Uri(activity.ServiceUrl));
            IConversationUpdateActivity update = activity;
            if (update.MembersAdded.Any())
            {
                var reply = activity.CreateReply();
                var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                foreach (var newMember in newMembers)
                {
                    reply.Text = "Welcome";
                    if (!string.IsNullOrEmpty(newMember.Name))
                    {
                        reply.Text += $" {newMember.Name}";
                    }
                    reply.Text += "!";
                    await client.Conversations.ReplyToActivityAsync(reply);
                }
            }
        }
    }
}
