using System.Text.Json;
using NetAdmin.Shared;

namespace NetAdmin.Client
{
    /// <summary>
    /// Gestionnaire d'authentification pour le client
    /// </summary>
    public class AuthenticationClient
    {
        private readonly NetworkClient _networkClient;
        private string _currentToken = "";
        private string _currentRefreshToken = "";
        private DateTime _tokenExpiresAt;
        private bool _isAuthenticated;

        public event Action<bool> OnAuthenticationChanged = delegate { };
        public event Action<string> OnAuthenticationError = delegate { };

        public bool IsAuthenticated => _isAuthenticated;
        public string CurrentToken => _currentToken;
        public string Username { get; private set; } = "";
        public int UserId { get; private set; }
        public string UserRole { get; private set; } = "";

        public AuthenticationClient(NetworkClient networkClient)
        {
            _networkClient = networkClient;
        }

        /// <summary>
        /// Se connecte avec un username et password
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var packet = NetworkPacket.Create(PacketType.Login, "CLIENT", loginRequest);
                await _networkClient.SendPacketAsync(packet);

                // TODO: Attendre la réponse LoginResponse
                // Pour l'instant, c'est une structure basique
                return false;
            }
            catch (Exception ex)
            {
                OnAuthenticationError?.Invoke($"Erreur de connexion: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Se déconnecte
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                // Envoyer une requête de déconnexion au serveur
                var packet = NetworkPacket.Create(PacketType.Logout, "CLIENT", new { });
                packet.AuthToken = _currentToken;
                await _networkClient.SendPacketAsync(packet);

                // Effacer les tokens localement
                ClearTokens();
                _isAuthenticated = false;
                OnAuthenticationChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                OnAuthenticationError?.Invoke($"Erreur de déconnexion: {ex.Message}");
            }
        }

        /// <summary>
        /// Traite une réponse de connexion réussie
        /// </summary>
        public void HandleLoginResponse(LoginResponse response)
        {
            if (response.Success)
            {
                _currentToken = response.Token;
                _currentRefreshToken = response.RefreshToken;
                _tokenExpiresAt = response.ExpiresAt;
                _isAuthenticated = true;
                Username = response.Username;
                UserId = response.UserId;
                UserRole = response.Role;

                OnAuthenticationChanged?.Invoke(true);
                Console.WriteLine($"✓ Connecté en tant que {response.Username} ({response.Role})");
            }
            else
            {
                OnAuthenticationError?.Invoke(response.Message);
                Console.WriteLine($"✗ Erreur de connexion: {response.Message}");
            }
        }

        /// <summary>
        /// Renouvelle le token si expirant
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentRefreshToken))
                {
                    OnAuthenticationError?.Invoke("Pas de refresh token disponible");
                    return false;
                }

                // Vérifier si le token est proche de l'expiration (moins de 5 minutes)
                if (DateTime.UtcNow.AddMinutes(5) < _tokenExpiresAt)
                {
                    return true; // Token encore valide
                }

                var refreshRequest = new RefreshTokenRequest
                {
                    Token = _currentToken,
                    RefreshToken = _currentRefreshToken
                };

                var packet = NetworkPacket.Create(PacketType.RefreshToken, "CLIENT", refreshRequest);
                await _networkClient.SendPacketAsync(packet);

                return true;
            }
            catch (Exception ex)
            {
                OnAuthenticationError?.Invoke($"Erreur de renouvellement: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ajoute le token d'authentification à un paquet
        /// </summary>
        public NetworkPacket AuthorizePacket(NetworkPacket packet)
        {
            if (!_isAuthenticated)
            {
                throw new InvalidOperationException("Client non authentifié");
            }

            packet.AuthToken = _currentToken;
            return packet;
        }

        /// <summary>
        /// Efface les tokens
        /// </summary>
        private void ClearTokens()
        {
            _currentToken = "";
            _currentRefreshToken = "";
            _tokenExpiresAt = DateTime.MinValue;
        }

        /// <summary>
        /// Vérifie si le token est valide
        /// </summary>
        public bool IsTokenValid()
        {
            return _isAuthenticated && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30);
        }
    }
}
