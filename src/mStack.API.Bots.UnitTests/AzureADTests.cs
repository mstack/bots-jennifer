using Autofac.Extras.Moq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using mStack.API.Bots.AzureAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.UnitTests
{
    [TestClass]
    public class AzureADTests
    {
        [TestInitialize]
        public void Initialize()
        {
            System.Environment.SetEnvironmentVariable("AD_CLIENT_ID", "4db3e943-ac1d-46cc-bbc1-b1a47a091492");
            System.Environment.SetEnvironmentVariable("AD_CLIENT_SECRET", "YDYDH1lxQIVV95MaDCG3yjzMOv8OwoZ/1AVR4Tl8ZbY=");
            System.Environment.SetEnvironmentVariable("AD_ENDPOINT_URL", "https://login.microsoftonline.com");
            System.Environment.SetEnvironmentVariable("AD_MODE", "V1");
            System.Environment.SetEnvironmentVariable("AD_REDIRECT_URL", "http://localhost:3978/api/oauth");
            System.Environment.SetEnvironmentVariable("AD_SCOPES", "");
            System.Environment.SetEnvironmentVariable("AD_RESOURCE", "https://graph.windows.net/");
            System.Environment.SetEnvironmentVariable("AD_TENANT", "49445e6c-4079-4692-8349-8bb3853f22fc");
        }

        [TestMethod]
        public async Task GetAuthUrlAync_Azure_Success()
        {
            ResumptionCookie cookie = new ResumptionCookie(new Address("botId", "channelId", "userId", "conversationId", "serviceUrl"), "userName", false, "en");
            var authUrl = await AzureADHelper.GetAuthUrlAsync(cookie, "https://test");

            string expected = "https://login.microsoftonline.com/49445e6c-4079-4692-8349-8bb3853f22fc/oauth2/authorize?resource=https:%2F%2Ftest&client_id=4db3e943-ac1d-46cc-bbc1-b1a47a091492&response_type=code&haschrome=1&redirect_uri=http:%2F%2Flocalhost:3978%2Fapi%2Foauth&x-client-SKU=PCL.Desktop&x-client-Ver=3.13.8.999&x-client-CPU=x64&x-client-OS=Microsoft+Windows+NT+6.3.9600.0&state=H4sIAAAAAAAEAFWMSw7CMAxELVfiu-BM0AWqhNhADxASS1QKMbKTrjkkB8JtEZ-V5z1r5gkA1TYEIVV4GOCOcxNgZvEyJqyvLiWKFlcm_YewVRK7c7Nlilhz6knU5Y6T8WYo_Cs8kfSdp1YirO2tX5wWj-5GsHxvjrBo9CxFM4Wf7mD3wuVutQN7FwkqK1ECeAE2vIiE1AAAAA2&response_mode=form_post";
            Assert.AreEqual(expected, authUrl);
        }

        [TestMethod]
        public async Task GetAuthToken_Azure_Success()
        {
            using (var autoMock = AutoMock.GetLoose())
            {
                AuthenticationSettings authenticationSettings = AuthenticationSettings.GetFromAppSettings();
                string resource = "https://test";

                Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult result = autoMock.Mock<Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult>().Object;

                autoMock.Mock<AuthenticationContext>()
                        .Setup(t => t.AcquireTokenSilentAsync(resource, authenticationSettings.ClientId))
                        .Returns(() => Task.FromResult(result));

                await AzureADHelper.GetToken("userUniqueId", authenticationSettings, resource);
            }
        }
    }
}
