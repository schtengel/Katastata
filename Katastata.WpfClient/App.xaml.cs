using Katastata.Services;
using System;
using System.Windows;

namespace Katastata
{
    public partial class App : System.Windows.Application
    {
        private const string DefaultApiUrl = "http://localhost:5099";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var apiUrl = Environment.GetEnvironmentVariable("KATASTATA_API_URL") ?? DefaultApiUrl;
            var apiClient = new ApiClient(apiUrl);

            int maxTries = 1;
            int tries = 0;
            bool authenticated = false;

            while (!authenticated && tries < maxTries)
            {
                var auth = new AuthWindow(apiClient);
                var dialogResult = auth.ShowDialog();

                if (dialogResult == true)
                {
                    authenticated = true;
                    var mainWindow = new MainWindow(auth.LoggedInUserId, apiClient);
                    Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                tries++;
            }

            if (!authenticated)
            {
                Shutdown();
            }
        }
    }
}
