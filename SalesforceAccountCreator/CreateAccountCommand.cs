using System;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Xml;

namespace SalesforceAccountCreator
{

    internal sealed class CreateAccountCommand
    {
        static string accountname = "Antony";
        static string username = "candidate@ses.com";
        static string password = "GuestPOC123";
        static string securityToken = "sTLZlCcOhtTWoPKzWNEMVQMx4";
        static string apiVersion = "v56.0"; 
        static string loginUrl = "https://login.salesforce.com/services/Soap/u/56.0";
        //static string loginUrl = "https://login.salesforce.com/services/oauth2/token";
        static string restEndpoint = "https://orgfarm-3ad4e46dbc-dev-ed.develop.my.salesforce.com/services/apexrest/AccountManager/CreateAccount";

        private const int CommandId = 0x0100;
        private static readonly Guid CommandSet = new Guid("eaa2df79-81a7-4849-bb12-1f46eed15232");
        private readonly AsyncPackage package;

        // Your name as a global static variable
        public static string UserName { get; set; } = "Antony";

        private CreateAccountCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(async (s, e) => await ExecuteAsync(), menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new CreateAccountCommand(package, commandService);
        }

        private async Task ExecuteAsync()
        {
            MessageBox.Show("Clicked");
            try
            {
                var (sessionId, instanceUrl) = await LoginToSalesforce();

                if (!string.IsNullOrEmpty(sessionId))
                {
                    await CallRestAPI(instanceUrl, sessionId);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex.Message);
            }
        }

        private async Task<(string sessionId, string instanceUrl)> LoginToSalesforce()
        {
            string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
            <env:Envelope xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                          xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                          xmlns:env=""http://schemas.xmlsoap.org/soap/envelope/"">
              <env:Body>
                <n1:login xmlns:n1=""urn:partner.soap.sforce.com"">
                  <n1:username>{username}</n1:username>
                  <n1:password>{password}{securityToken}</n1:password>
                </n1:login>
              </env:Body>
            </env:Envelope>";

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "text/xml; charset=UTF-8");
                content.Headers.Add("SOAPAction", "login");

                HttpResponseMessage response = await client.PostAsync(loginUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(responseString);

                    var sessionId = xml.GetElementsByTagName("sessionId")[0]?.InnerText;
                    var serverUrl = xml.GetElementsByTagName("serverUrl")[0]?.InnerText;

                    Uri uri = new Uri(serverUrl);
                    string instanceUrl = uri.GetLeftPart(UriPartial.Authority);

                    return (sessionId, instanceUrl);
                }
                else
                {
                    throw new Exception("Login failed: " + responseString);
                }
            }
        }

        private async Task CallRestAPI(string instanceUrl, string sessionId)
        {
            //string endpoint = "https://orgfarm-3ad4e46dbc-dev-ed.develop.my.salesforce.com/services/apexrest/AccountManager/CreateAccount";
            //string endpoint = instanceUrl + "/services/apexrest/AccountManager/CreateAccount";
            string endpoint = $"{instanceUrl}/services/data/{apiVersion}/sobjects/Account/";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", sessionId);

                //var jsonBody = $"{{ \"Name\": \"{accountname}\" }}";
                var jsonBody = "{\"Name\": \"Antony\"}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(endpoint, content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowMessage("Account created successfully.");
                }
                else
                {
                    ShowMessage("Failed to create account: " + result);
                }
            }
        }

        private void ShowMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "Salesforce Integration",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
