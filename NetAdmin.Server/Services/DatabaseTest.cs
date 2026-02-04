using NetAdmin.Server.Data;
using NetAdmin.Server.Data.Entities;

namespace NetAdmin.Server.Services
{
    /// <summary>
    /// Utilitaire pour tester et afficher l'état de la base de données
    /// </summary>
    public static class DatabaseTest
    {
        public static void DisplayDatabaseStatus()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    Console.WriteLine("\n=== ÉTAT DE LA BASE DE DONNÉES ===\n");

                    // Afficher les utilisateurs
                    var users = context.Users.ToList();
                    Console.WriteLine($"[DB] Total d'utilisateurs: {users.Count}");
                    
                    foreach (var user in users)
                    {
                        Console.WriteLine($"  - {user.Username} ({user.Role}) - Email: {user.Email} - Actif: {user.IsActive}");
                    }

                    // Afficher les tokens
                    var tokens = context.AuthTokens.ToList();
                    Console.WriteLine($"\n[DB] Total de tokens: {tokens.Count}");
                    
                    foreach (var token in tokens)
                    {
                        Console.WriteLine($"  - Token UserId {token.UserId} - Actif: {token.IsActive} - Expire: {token.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                    }

                    // Afficher les statistiques
                    Console.WriteLine($"\n[DB] Statistiques:");
                    Console.WriteLine($"  - Logs d'audit: {context.AuditLogs.Count()}");
                    Console.WriteLine($"  - Clients enregistrés: {context.ClientHosts.Count()}");
                    Console.WriteLine($"  - Logs de metrics: {context.MetricLogs.Count()}");

                    Console.WriteLine("\n=== FIN DU RAPPORT ===\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
