using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using System.Net;
using System.Collections.Specialized;
using mStack.API.Bots.Auth;
using mStack.API.Bots.AzureAD;

namespace mStack.API.Bots.SharePoint
{
    public static class SharePointHelper
    {
        internal static Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, string siteRelativeUrl, string authScope)
        {
            // https://mstackbv.sharepoint.com/sites/processes/?client_id=f485755d-217f-4d4f-a3ba-395af4d73d3e&scope=AllSites.Read&response_type=code&redirect_uri=https%3A%2F%2Flocalhost%3A3978%2Fapi%2Foauth

            SharePointSettings settings = SharePointSettings.GetFromAppSettings();
            string authUri = $"{settings.TenantUrl}{siteRelativeUrl}/_layouts/15/OAuthAuthorize.aspx";

            var state = AuthUtilities.EncodeResumptionCookie(resumptionCookie);

            NameValueCollection queryParams = new NameValueCollection();
            queryParams.Add("client_id", settings.ClientId);
            queryParams.Add("scope", authScope);
            queryParams.Add("response_type", "code");
            queryParams.Add("redirect_uri", settings.RedirectUrl);
            queryParams.Add("state", state);

            authUri += AuthUtilities.ToQueryString(queryParams);
            return Task.FromResult(authUri);
        }
        

        //internal static async Task<AuthenticationResult> GetRefreshToken(IDialogContext context, string refreshToken)
        //{
        //    SharePointSettings settings = SharePointSettings.GetFromAppSettings();

        //    string spPrinciple = "00000003-0000-0ff1-ce00-000000000000";
        //    string spAuthUrl = "https://accounts.accesscontrol.windows.net/" + settings.TenantId + "/tokens/OAuth/2";

        //    KeyValuePair<string, string>[] body = new KeyValuePair<string, string>[]
        //    {
        //        new KeyValuePair<string, string>("grant_type", "refresh_token"),
        //        new KeyValuePair<string, string>("client_id", $"{settings.ClientId}@{settings.TenantId}"),
        //        new KeyValuePair<string, string>("resource", $"{spPrinciple}/{settings.TenantUrl}@{settings.TenantId}".Replace("https://", "")),
        //        new KeyValuePair<string, string>("client_secret", settings.ClientSecret),
        //        new KeyValuePair<string, string>("refresh_token", refreshToken),
        //        new KeyValuePair<string, string>("redirect_uri", settings.RedirectUrl)
        //    };

        //    var content = new FormUrlEncodedContent(body);
        //    var contentLength = content.ToString().Length;

        //    AuthenticationResult result = new AuthenticationResult();

        //    HttpClient client = new HttpClient();
        //    using (HttpResponseMessage response = await client.PostAsync(spAuthUrl, content))
        //    {
        //        if (response.Content != null)
        //        {
        //            string responseString = await response.Content.ReadAsStringAsync();
        //            JObject data = JObject.Parse(responseString);

        //            result.AccessToken = data.Value<string>("access_token");
        //            result.ExpiresOnUtcTicks = data.Value<long>("expires_on");
        //            result.Resource = data.Value<string>("resource");
        //            TODO: need to extend the result with more fields when available
        //        }
        //    }

        //    return result;
        //}

        public static async Task<AuthenticationResult> GetTokenByAuthCodeAsync(string code)
        {
            SharePointSettings settings = SharePointSettings.GetFromAppSettings();

            string spPrinciple = "00000003-0000-0ff1-ce00-000000000000";
            string spAuthUrl = "https://accounts.accesscontrol.windows.net/" + settings.TenantId + "/tokens/OAuth/2";

            KeyValuePair<string, string>[] body = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", $"{settings.ClientId}@{settings.TenantId}"),
                new KeyValuePair<string, string>("resource", $"{spPrinciple}/{settings.TenantUrl}@{settings.TenantId}".Replace("https://", "")),
                new KeyValuePair<string, string>("client_secret", settings.ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", settings.RedirectUrl)
            };

            var content = new FormUrlEncodedContent(body);
            var contentLength = content.ToString().Length;

            AuthenticationResult result = new AuthenticationResult();

            HttpClient client = new HttpClient();
            using (HttpResponseMessage response = await client.PostAsync(spAuthUrl, content))
            {
                if (response.Content != null)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(responseString);

                    result.AccessToken = data.Value<string>("access_token");
                    result.RefreshToken = data.Value<string>("refresh_token");
                    result.ExpiresOnUtcTicks = data.Value<long>("expires_on");
                    // TODO: need to extend the result with more fields when available
                }
            }

            return result;
        }

        public static async Task<string> GetDigestForSharePoint(string siteUrl, string token)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            client.DefaultRequestHeaders.Add("accept", "application/json;odata=verbose");
            StringContent content = new StringContent("");

            string spTenantUrl = System.Configuration.ConfigurationManager.AppSettings["SP_TENANT_URL"];
            string digest = "";

            using (HttpResponseMessage response = await client.PostAsync($"{spTenantUrl}{siteUrl}/_api/contextinfo", content))
            {
                if (response.IsSuccessStatusCode)
                {
                    string contentJson = response.Content.ReadAsStringAsync().Result;
                    JObject val = JObject.Parse(contentJson);
                    JToken d = val["d"];
                    JToken wi = d["GetContextWebInformation"];
                    digest = wi.Value<string>("FormDigestValue");
                }
            }

            return digest;
        }
    }
}
