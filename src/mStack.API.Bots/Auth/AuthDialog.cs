// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace mStack.API.Bots.Auth
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Autofac;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Diagnostics;

    [Serializable]
    public abstract class AuthDialog : IDialog<string>
    {
        protected string prompt { get; }

        public abstract string dialogId { get; }
        public abstract Task<string> GetAuthUrl(ResumptionCookie resumptionCookie);
        public abstract Task<string> GetAccessToken(IDialogContext context);

        public AuthDialog(string prompt = "Please click to sign in: ")
        {
            this.prompt = prompt;
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

            try
            {
                if (context.UserData.TryGetValue(dialogId + '_' + AuthenticationConstants.AuthResultKey, out authResult))
                {
                    try
                    {
                        //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                        //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                        //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                        context.UserData.TryGetValue<string>(dialogId + '_' + AuthenticationConstants.MagicNumberValidated, out validated);
                        if (validated == "true")
                        {
                            context.Done($"Thanks! You are now logged in. ");
                        }
                        else if (context.UserData.TryGetValue<int>(dialogId + '_' + AuthenticationConstants.MagicNumberKey, out magicNumber))
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
                                    context.UserData.SetValue<string>(dialogId + '_' + AuthenticationConstants.MagicNumberValidated, "true");
                                    context.Done($"Thanks! You are now logged in. ");
                                }
                                else
                                {
                                    context.UserData.RemoveValue(dialogId + '_' + AuthenticationConstants.AuthResultKey);
                                    context.UserData.SetValue<string>(dialogId + '_' + AuthenticationConstants.MagicNumberValidated, "false");
                                    context.UserData.RemoveValue(dialogId + '_' + AuthenticationConstants.MagicNumberKey);
                                    await context.PostAsync($"I'm sorry but I couldn't validate your number. Please try authenticating once again. ");

                                    context.Wait(this.MessageReceivedAsync);
                                }
                            }
                        }
                    }
                    catch
                    {
                        context.UserData.RemoveValue(dialogId + '_' + AuthenticationConstants.AuthResultKey);
                        context.UserData.SetValue(dialogId + '_' + AuthenticationConstants.MagicNumberValidated, "false");
                        context.UserData.RemoveValue(dialogId + '_' + AuthenticationConstants.MagicNumberKey);
                        context.Done($"I'm sorry but something went wrong while authenticating.");
                    }
                }
                else
                {
                    await this.LogIn(context, msg);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Could not process the received authdialog message: {ex}");
                throw;
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
                        SigninCard plCard = new SigninCard(this.prompt, GetCardActions(authenticationUrl, "signin"));
                        plAttachment = plCard.ToAttachment();
                        break;
                    }
                // Teams does not yet support signin cards
                case "msteams":
                    {
                        ThumbnailCard plCard = new ThumbnailCard()
                        {
                            Title = this.prompt,
                            Subtitle = "",
                            Images = new List<CardImage>(),
                            Buttons = GetCardActions(authenticationUrl, "openUrl")
                        };
                        plAttachment = plCard.ToAttachment();
                        break;
                    }
                default:
                    return context.PostAsync(this.prompt + "[Click here](" + authenticationUrl + ")");
            }

            IMessageActivity response = context.MakeMessage();
            response.Recipient = msg.From;
            response.Type = "message";

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(plAttachment);

            // set the dialog id so the auth callback can find out which type of callback it's receiving
            context.UserData.SetValue(AuthenticationConstants.AuthDialogIdKey, dialogId);

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
                string token = await GetAccessToken(context);

                if (string.IsNullOrEmpty(token))
                {
                    if (msg.Text != null &&
                        CancellationWords.GetCancellationWords().Contains(msg.Text.ToUpper()))
                    {
                        context.Done(string.Empty);
                    }
                    else
                    {
                        // storing the dialog id as active dialog so the auth callback handler can resolve the correct handler
                        context.UserData.SetValue(AuthenticationConstants.AuthHandlerKey, this.dialogId);
                        context.UserData.SetValue(dialogId + '_' + AuthenticationConstants.OriginalMessageText, msg.Text);

                        var resumptionCookie = new ResumptionCookie(msg);

                        string authenticationUrl = await GetAuthUrl(resumptionCookie);

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


//*********************************************************
//
//AuthBot, https://github.com/microsoftdx/AuthBot
//
//Copyright (c) Microsoft Corporation
//All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ""Software""), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:




// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.




// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//*********************************************************
