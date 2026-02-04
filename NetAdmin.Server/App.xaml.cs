using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using NetAdmin.Server.Data;
using NetAdmin.Server.Services;

namespace NetAdmin.Server;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            // Initialisation de la base de données
            var dbContext = new AppDbContext();
            var authService = new AuthenticationService(dbContext, "a-very-secret-key-that-should-be-in-config");
            var dbInitializer = new DatabaseInitializer(dbContext, authService);
            dbInitializer.Initialize();

            // LoginWindow sera créée par StartupUri dans App.xaml
            // La MainWindow sera créée après l'authentification réussie
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STARTUP EXCEPTION");
            sb.AppendLine(ex.ToString());
            
            try
            {
                File.AppendAllText("netadmin.startup.log", sb.ToString());
                System.Windows.MessageBox.Show($"Startup Error:\n{ex.Message}\n\n{ex.InnerException?.Message}", "NetAdmin Error");
            }
            catch { }
            
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatal(e.Exception, "UI");
        e.Handled = true;
        Shutdown(-1);
    }

    private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowFatal(ex, "Domain");
        }
        else
        {
            ShowFatal(new Exception("Unhandled non-Exception error."), "Domain");
        }
    }

    private static void ShowFatal(Exception ex, string scope)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {scope} fatal error");
            builder.AppendLine(ex.ToString());
            builder.AppendLine();

            File.AppendAllText("netadmin.startup.log", builder.ToString());

            MessageBox.Show(
                $"{scope} error:\n{ex.Message}\n\nDetails saved to netadmin.startup.log",
                "NetAdmin Server",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Last-resort: do not throw from error handler.
        }
    }
}
