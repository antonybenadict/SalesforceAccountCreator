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

namespace SalesforceAccountCreator
{
    internal sealed class CreateAccountCommand
    {
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
            const string url = "https://orgfarm-3ad4e46dbc-dev-ed.develop.my.salesforce.com/services/apexrest/AccountManager/CreateAccount";
            const string username = "candidate@ses.com";
            const string password = "GuestPOC123";
            const string token = "sTLZlCcOhtTWoPKzWNEMVQMx4";
            string fullPassword = password + token;


            try
            {
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                var client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var accountData = new { Name = UserName };
                var json = JsonConvert.SerializeObject(accountData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");


                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                VsShellUtilities.ShowMessageBox(
                    package,
                    $"Response Status: {response.StatusCode}\nResponse Body: {responseContent}",
                    "Salesforce Webhook Response",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    $"Error sending request: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
