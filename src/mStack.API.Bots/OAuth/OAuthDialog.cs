// Partially based on the AuthBot sample by Microsoft. See original at https://github.com/microsoftdx/AuthBot

namespace mStack.API.Bots.OAuth
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Autofac;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    [Serializable]
    public class OAuthDialog : IDialog<string>
    {
        AuthenticationRequest _request;

        public OAuthDialog(AuthenticationRequest request)
        {
            this._request = request;                
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;

            AuthenticationResult authResult;
            string validated = "";
            int magicNumber = 0;
            if (context.UserData.TryGetValue(AuthenticationConstants.AuthResultKey, out authResult))
            {
                try
                {
                    //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                    //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                    //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                    context.UserData.TryGetValue<string>(AuthenticationConstants.MagicNumberValidated, out validated);
                    if (validated == "true")
                    {
                        context.Done($"Thanks {authResult.UserName}. You are now logged in. ");
                    }
                    else if (context.UserData.TryGetValue<int>(AuthenticationConstants.MagicNumberKey, out magicNumber))
                    {
                        if (msg.Text == null)
                        {
                            await context.PostAsync($"Please paste back the number you received in your authentication screen.");

                            context.Wait(this.MessageReceivedAsync);
                        }
                        else
                        {

                            if (msg.Text.Length >= 6 && magicNumber.ToString() == msg.Text.Substring(0, 6))
                            {
                                context.UserData.SetValue<string>(AuthenticationConstants.MagicNumberValidated, "true");
                                context.Done($"Thanks {authResult.UserName}. You are now logged in. ");
                            }
                            else
                            {
                                context.UserData.RemoveValue(AuthenticationConstants.AuthResultKey);
                                context.UserData.SetValue<string>(AuthenticationConstants.MagicNumberValidated, "false");
                                context.UserData.RemoveValue(AuthenticationConstants.MagicNumberKey);
                                await context.PostAsync($"I'm sorry but I couldn't validate your number. Please try authenticating once again. ");

                                context.Wait(this.MessageReceivedAsync);
                            }
                        }
                    }
                }
                catch
                {
                    context.UserData.RemoveValue(AuthenticationConstants.AuthResultKey);
                    context.UserData.SetValue(AuthenticationConstants.MagicNumberValidated, "false");
                    context.UserData.RemoveValue(AuthenticationConstants.MagicNumberKey);
                    context.Done($"I'm sorry but something went wrong while authenticating.");
                }
            }
            else
            {
                await this.LogIn(context, msg);
            }
        }

        /// <summary>
        /// Prompts the user to login. This can be overridden inorder to allow custom prompt messages or cards per channel.
        /// </summary>
        /// <param name="context">Chat context</param>
        /// <param name="msg">Chat message</param>
        /// <param name="authenticationUrl">OAuth URL for authenticating user</param>
        /// <returns>Task from Posting or prompt to the context.</returns>
        protected virtual Task PromptToLogin(IDialogContext context, IMessageActivity msg, string authenticationUrl)
        {
            Attachment plAttachment = null;
            switch (msg.ChannelId)
            {
                case "emulator":
                case "skype":
                    {
                        SigninCard plCard = new SigninCard(this._request.Prompt, GetCardActions(authenticationUrl, "signin"));
                        plAttachment = plCard.ToAttachment();
                        break;
                    }
                // Teams does not yet support signin cards
                case "msteams":
                    {
                        ThumbnailCard plCard = new ThumbnailCard()
                        {
                            Title = this._request.Prompt,
                            Subtitle = "",
                            Images = new List<CardImage>(),
                            Buttons = GetCardActions(authenticationUrl, "openUrl")
                        };
                        plAttachment = plCard.ToAttachment();
                        break;
                    }
                default:
                    return context.PostAsync(this._request.Prompt + "[Click here](" + authenticationUrl + ")");
            }

            IMessageActivity response = context.MakeMessage();
            response.Recipient = msg.From;
            response.Type = "message";

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(plAttachment);

            return context.PostAsync(response);
        }

        private List<CardAction> GetCardActions(string authenticationUrl, string actionType)
        {
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction plButton = new CardAction()
            {
                Value = authenticationUrl,
                Type = actionType,
                Title = "Authentication Required"
            };
            cardButtons.Add(plButton);
            return cardButtons;
        }

        private async Task LogIn(IDialogContext context, IMessageActivity msg)
        {
            try
            {
                string token = await context.GetAccessToken(_request);

                if (string.IsNullOrEmpty(token))
                {
                    if (msg.Text != null &&
                        CancellationWords.GetCancellationWords().Contains(msg.Text.ToUpper()))
                    {
                        context.Done(string.Empty);
                    }
                    else
                    {
                        var resumptionCookie = new ResumptionCookie(msg);

                        string authenticationUrl = await AuthenticationHandlerFactory.GetAuthUrlAsync(resumptionCookie, _request);

                        await PromptToLogin(context, msg, authenticationUrl);
                        context.Wait(this.MessageReceivedAsync);
                    }
                }
                else
                {
                    context.Done(string.Empty);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}