using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data;
using NetAdmin.Server.Data.Entities;

namespace NetAdmin.Server.Services
{
    /// <summary>
    /// Service d'initialisation de la base de données
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly AppDbContext _context;
        private readonly AuthenticationService _authService;

        public DatabaseInitializer(AppDbContext context, AuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        /// <summary>
        /// Initialise la base de données et crée les utilisateurs par défaut
        /// </summary>
        public void Initialize()
        {
            try
            {
                Console.WriteLine("[DB] Vérification du schéma...");
                // S'assurer que la base de données et le schéma existent
                bool created = _context.Database.EnsureCreated();
                
                if (created)
                {
                    Console.WriteLine("[DB] Nouvelle base de données créée.");
                }
                else
                {
                    Console.WriteLine("[DB] Base de données existante détectée.");
                }

                // Vérifier si la base de données est vide
                if (_context.Users.Any())
                {
                    Console.WriteLine("[DB] Base de données déjà initialisée.");
                    return;
                }

                Console.WriteLine("[DB] Initialisation de la base de données...");

                // Créer l'utilisateur administrateur par défaut
                _authService.CreateUser(
                    username: "admin",
                    password: "Admin@123!",  // À changer lors de la première connexion
                    email: "admin@netadminpro.local",
                    fullName: "Administrateur Système",
                    role: UserRole.Administrator
                );

                // Créer un utilisateur superviseur
                _authService.CreateUser(
                    username: "supervisor",
                    password: "Supervisor@123!",
                    email: "supervisor@netadminpro.local",
                    fullName: "Superviseur",
                    role: UserRole.Supervisor
                );

                // Créer un utilisateur opérateur
                _authService.CreateUser(
                    username: "operator",
                    password: "Operator@123!",
                    email: "operator@netadminpro.local",
                    fullName: "Opérateur",
                    role: UserRole.Operator
                );

                // Créer un utilisateur lecteur
                _authService.CreateUser(
                    username: "viewer",
                    password: "Viewer@123!",
                    email: "viewer@netadminpro.local",
                    fullName: "Lecteur",
                    role: UserRole.Viewer
                );

                Console.WriteLine("[DB] Utilisateurs par défaut créés avec succès!");
                Console.WriteLine("[DB] Identifiants disponibles:");
                Console.WriteLine("     - admin / Admin@123!");
                Console.WriteLine("     - supervisor / Supervisor@123!");
                Console.WriteLine("     - operator / Operator@123!");
                Console.WriteLine("     - viewer / Viewer@123!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERREUR] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Réinitialise complètement la base de données
        /// </summary>
        public void Reset()
        {
            try
            {
                _context.Database.EnsureDeleted();
                Initialize();
                Console.WriteLine("[DB] Base de données réinitialisée avec succès!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERREUR] Impossible de réinitialiser la base: {ex.Message}");
            }
        }
    }
}
