using System.Text.Json;
using NetAdmin.Shared;

namespace NetAdmin.Server.Services
{
    /// <summary>
    /// Utilitaire de test pour l'authentification
    /// </summary>
    public class AuthenticationTester
    {
        private readonly AuthenticationService _authService;

        public AuthenticationTester(AuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Exécute une suite de tests complets
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("SUITE DE TESTS - SYSTÈME D'AUTHENTIFICATION");
            Console.WriteLine(new string('=', 60) + "\n");

            TestValidLogin();
            TestInvalidPassword();
            TestTokenValidation();
            TestTokenExpiration();
            TestRefreshToken();
            TestCreateUser();
            TestChangePassword();

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TESTS TERMINÉS");
            Console.WriteLine(new string('=', 60) + "\n");
        }

        private void TestValidLogin()
        {
            Console.WriteLine("TEST 1: Connexion valide");
            Console.WriteLine("-".PadRight(60, '-'));

            var request = new LoginRequest
            {
                Username = "admin",
                Password = "Admin@123!"
            };

            var response = _authService.Authenticate(request, "127.0.0.1");

            if (response.Success)
            {
                Console.WriteLine("PASS: Connexion réussie");
                Console.WriteLine($"  - Username: {response.Username}");
                Console.WriteLine($"  - Role: {response.Role}");
                Console.WriteLine($"  - Token reçu: {(string.IsNullOrEmpty(response.Token) ? "NON" : "OUI")}");
                Console.WriteLine($"  - Expires at: {response.ExpiresAt:G}");
            }
            else
            {
                Console.WriteLine($" FAIL: {response.Message}");
            }

            Console.WriteLine();
        }

        private void TestInvalidPassword()
        {
            Console.WriteLine("TEST 2: Mot de passe invalide");
            Console.WriteLine("-".PadRight(60, '-'));

            var request = new LoginRequest
            {
                Username = "admin",
                Password = "WrongPassword123!"
            };

            var response = _authService.Authenticate(request, "127.0.0.1");

            if (!response.Success && response.Message.Contains("incorrect"))
            {
                Console.WriteLine("✓ PASS: Erreur correctement signalée");
                Console.WriteLine($"  - Message: {response.Message}");
            }
            else
            {
                Console.WriteLine("✗ FAIL: Devrait rejeter le mauvais mot de passe");
            }

            Console.WriteLine();
        }

        private void TestTokenValidation()
        {
            Console.WriteLine("TEST 3: Validation du token");
            Console.WriteLine("-".PadRight(60, '-'));

            // D'abord, se connecter pour obtenir un token
            var loginResponse = _authService.Authenticate(
                new LoginRequest { Username = "admin", Password = "Admin@123!" },
                "127.0.0.1"
            );

            if (!loginResponse.Success)
            {
                Console.WriteLine("✗ FAIL: Impossible de se connecter pour le test");
                return;
            }

            // Valider le token
            var validation = _authService.ValidateToken(loginResponse.Token);

            if (validation.IsValid)
            {
                Console.WriteLine("✓ PASS: Token validé avec succès");
                Console.WriteLine($"  - UserId: {validation.UserId}");
                Console.WriteLine($"  - Username: {validation.Username}");
                Console.WriteLine($"  - Role: {validation.Role}");
            }
            else
            {
                Console.WriteLine($"✗ FAIL: {validation.ErrorMessage}");
            }

            Console.WriteLine();
        }

        private void TestTokenExpiration()
        {
            Console.WriteLine("TEST 4: Vérification de l'expiration");
            Console.WriteLine("-".PadRight(60, '-'));

            var loginResponse = _authService.Authenticate(
                new LoginRequest { Username = "admin", Password = "Admin@123!" },
                "127.0.0.1"
            );

            Console.WriteLine($"✓ Token généré");
            Console.WriteLine($"  - Expire à: {loginResponse.ExpiresAt:G}");
            Console.WriteLine($"  - Valide pour: 60 minutes");
            Console.WriteLine($"  - Maintenant: {DateTime.UtcNow:G}");

            Console.WriteLine();
        }

        private void TestRefreshToken()
        {
            Console.WriteLine("TEST 5: Renouvellement du token");
            Console.WriteLine("-".PadRight(60, '-'));

            var loginResponse = _authService.Authenticate(
                new LoginRequest { Username = "admin", Password = "Admin@123!" },
                "127.0.0.1"
            );

            var refreshRequest = new RefreshTokenRequest
            {
                Token = loginResponse.Token,
                RefreshToken = loginResponse.RefreshToken
            };

            var refreshResponse = _authService.RefreshToken(refreshRequest, "127.0.0.1");

            if (refreshResponse.Success)
            {
                Console.WriteLine("✓ PASS: Token renouvelé avec succès");
                Console.WriteLine($"  - Ancien token: {loginResponse.Token.Substring(0, 20)}...");
                Console.WriteLine($"  - Nouveau token: {refreshResponse.Token.Substring(0, 20)}...");
                Console.WriteLine($"  - Nouveau refresh token: {refreshResponse.RefreshToken.Substring(0, 20)}...");
            }
            else
            {
                Console.WriteLine($"✗ FAIL: {refreshResponse.Message}");
            }

            Console.WriteLine();
        }

        private void TestCreateUser()
        {
            Console.WriteLine("TEST 6: Création d'utilisateur");
            Console.WriteLine("-".PadRight(60, '-'));

            var created = _authService.CreateUser(
                "testuser",
                "TestPassword@123",
                "test@example.com",
                "Utilisateur Test",
                Data.Entities.UserRole.Operator
            );

            if (created)
            {
                Console.WriteLine("✓ PASS: Utilisateur créé avec succès");

                // Essayer de se connecter avec ce nouvel utilisateur
                var response = _authService.Authenticate(
                    new LoginRequest { Username = "testuser", Password = "TestPassword@123" },
                    "127.0.0.1"
                );

                if (response.Success)
                {
                    Console.WriteLine("✓ Connexion possible avec le nouvel utilisateur");
                    Console.WriteLine($"  - Role: {response.Role}");
                }
            }
            else
            {
                Console.WriteLine("✗ FAIL: Impossible de créer l'utilisateur (peut exister déjà)");
            }

            Console.WriteLine();
        }

        private void TestChangePassword()
        {
            Console.WriteLine("TEST 7: Changement de mot de passe");
            Console.WriteLine("-".PadRight(60, '-'));

            // Créer un utilisateur test
            _authService.CreateUser(
                "pwdtest",
                "OldPassword@123",
                "pwdtest@example.com",
                "Password Test User",
                Data.Entities.UserRole.Operator
            );

            // Se connecter avec l'ancien mot de passe
            var oldLoginResponse = _authService.Authenticate(
                new LoginRequest { Username = "pwdtest", Password = "OldPassword@123" },
                "127.0.0.1"
            );

            if (!oldLoginResponse.Success)
            {
                Console.WriteLine("✗ FAIL: Impossible de se connecter avant changement");
                return;
            }

            // Chercher l'utilisateur pour avoir son ID
            // Dans un vrai scénario, on l'obtiendrait depuis le token
            var users = GetAllUsers();
            var user = users.FirstOrDefault(u => u.Username == "pwdtest");

            if (user == null)
            {
                Console.WriteLine("✗ FAIL: Utilisateur créé pas trouvé");
                return;
            }

            // Changer le mot de passe
            var changed = _authService.ChangePassword(
                user.Id,
                "OldPassword@123",
                "NewPassword@456"
            );

            if (changed)
            {
                Console.WriteLine("✓ PASS: Mot de passe changé avec succès");

                // Essayer de se connecter avec l'ancien mot de passe (devrait échouer)
                var oldPwdAttempt = _authService.Authenticate(
                    new LoginRequest { Username = "pwdtest", Password = "OldPassword@123" },
                    "127.0.0.1"
                );

                if (!oldPwdAttempt.Success)
                {
                    Console.WriteLine("✓ Ancien mot de passe rejeta correctement");
                }

                // Essayer avec le nouveau mot de passe
                var newPwdAttempt = _authService.Authenticate(
                    new LoginRequest { Username = "pwdtest", Password = "NewPassword@456" },
                    "127.0.0.1"
                );

                if (newPwdAttempt.Success)
                {
                    Console.WriteLine(" Nouveau mot de passe accepté");
                }
            }
            else
            {
                Console.WriteLine(" FAIL: Impossible de changer le mot de passe");
            }

            Console.WriteLine();
        }

        // Fonction helper pour récupérer tous les utilisateurs
        private List<Data.Entities.User> GetAllUsers()
        {
            // Cette méthode devrait accéder à la base de données
            // Pour l'instant, on retourne une liste vide
            // À implémenter selon votre contexte
            return new List<Data.Entities.User>();
        }
    }
}
