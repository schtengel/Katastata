using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Katastata.Data;

namespace Katastata
{
    public partial class App : System.Windows.Application
    {
        private const string DbFileName = "katastata.db";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Directory.CreateDirectory(Katastata.Helpers.AppPaths.DataDir);
            var legacyDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
            if (!File.Exists(Katastata.Helpers.AppPaths.DbPath) && File.Exists(legacyDb))
                File.Copy(legacyDb, Katastata.Helpers.AppPaths.DbPath);
            var dbPath = Katastata.Helpers.AppPaths.DbPath;
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using (var ctx = new AppDbContext(options))
                ctx.Database.EnsureCreated();

            int maxTries = 1;
            int tries = 0;
            bool authenticated = false;

            while (!authenticated && tries < maxTries)
            {
                var auth = new AuthWindow(options);
                var dialogResult = auth.ShowDialog();

                if (dialogResult == true)
                {
                    authenticated = true;
                    var mainWindow = new MainWindow(auth.LoggedInUserId, options);
                    Current.MainWindow = mainWindow;  // Установите явно для безопасности
                    mainWindow.Show();
                    Current.ShutdownMode = ShutdownMode.OnMainWindowClose;  // Верните нормальный режим
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