using System.Windows;
using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data;
using NetAdmin.Server.Services;

namespace NetAdmin.Server;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // === INITIALISATION DE LA BASE DE DONNÉES ===
            Console.WriteLine("[STARTUP] Initialisation du système...");
            
            // Lire la configuration JWT
            string jwtSecret = "your-super-secret-key-min-32-characters-for-security";
            int tokenExpirationMinutes = 60;
            int refreshTokenExpirationDays = 7;
            
            Console.WriteLine("[STARTUP] Configuration JWT chargée");
            
            // Créer le contexte de la base de données
            using (var context = new AppDbContext())
            {
                Console.WriteLine("[STARTUP] Contexte de base de données créé");
                
                // Créer les services
                var authService = new AuthenticationService(context, jwtSecret, tokenExpirationMinutes, refreshTokenExpirationDays);
                var databaseInitializer = new DatabaseInitializer(context, authService);
                
                Console.WriteLine("[STARTUP] Services créés");
                
            // Initialiser la base de données et les utilisateurs par défaut
                try
                {
                    databaseInitializer.Initialize();
                    Console.WriteLine("[STARTUP] Base de données initialisée avec succès");
                    
                    // Afficher le statut
                    DatabaseTest.DisplayDatabaseStatus();
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"[STARTUP] Erreur d'initialisation DB: {dbEx.Message}");
                    throw;
                }
            }

            // === DÉMARRAGE DE L'APPLICATION WPF ===
            Console.WriteLine("[STARTUP] Démarrage de l'interface utilisateur...");
            
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur critique: {ex.Message}\n\n{ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            Console.WriteLine($"[ERREUR] {ex}");
        }
    }
}
