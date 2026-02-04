using NetAdmin.Server.Data;
using NetAdmin.Server.Data.Entities;
using NetAdmin.Shared;

namespace NetAdmin.Server.Services
{
    /// <summary>
    /// Gestionnaire de sessions authentifiées pour les clients
    /// </summary>
    public class AuthenticatedClientSession
    {
        public string ClientId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string AuthToken { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gestionnaire des sessions authentifiées
    /// </summary>
    public class SessionManager
    {
        private readonly Dictionary<string, AuthenticatedClientSession> _sessions = new();
        private readonly object _lockObject = new object();

        public event Action<AuthenticatedClientSession> OnSessionCreated = delegate { };
        public event Action<string> OnSessionClosed = delegate { };

        /// <summary>
        /// Crée une nouvelle session authentifiée
        /// </summary>
        public void CreateSession(string clientId, LoginResponse loginResponse, string ipAddress)
        {
            lock (_lockObject)
            {
                var session = new AuthenticatedClientSession
                {
                    ClientId = clientId,
                    UserId = loginResponse.UserId,
                    Username = loginResponse.Username,
                    Role = Enum.Parse<UserRole>(loginResponse.Role),
                    AuthToken = loginResponse.Token,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                _sessions[clientId] = session;
                OnSessionCreated?.Invoke(session);
            }
        }

        /// <summary>
        /// Récupère une session
        /// </summary>
        public AuthenticatedClientSession GetSession(string clientId)
        {
            lock (_lockObject)
            {
                _sessions.TryGetValue(clientId, out var session);
                if (session != null)
                {
                    session.LastActivityAt = DateTime.UtcNow;
                }
                return session!;
            }
        }

        /// <summary>
        /// Vérifie si un client est authentifié
        /// </summary>
        public bool IsAuthenticated(string clientId)
        {
            lock (_lockObject)
            {
                return _sessions.ContainsKey(clientId);
            }
        }

        /// <summary>
        /// Ferme une session
        /// </summary>
        public void CloseSession(string clientId)
        {
            lock (_lockObject)
            {
                if (_sessions.Remove(clientId))
                {
                    OnSessionClosed?.Invoke(clientId);
                }
            }
        }

        /// <summary>
        /// Récupère tous les clients authentifiés
        /// </summary>
        public List<AuthenticatedClientSession> GetActiveSessions()
        {
            lock (_lockObject)
            {
                return _sessions.Values.ToList();
            }
        }

        /// <summary>
        /// Nombre de sessions actives
        /// </summary>
        public int ActiveSessionCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _sessions.Count;
                }
            }
        }

        /// <summary>
        /// Obtient les sessions par rôle
        /// </summary>
        public List<AuthenticatedClientSession> GetSessionsByRole(UserRole role)
        {
            lock (_lockObject)
            {
                return _sessions.Values.Where(s => s.Role == role).ToList();
            }
        }

        /// <summary>
        /// Obtient les sessions par utilisateur
        /// </summary>
        public List<AuthenticatedClientSession> GetSessionsByUser(int userId)
        {
            lock (_lockObject)
            {
                return _sessions.Values.Where(s => s.UserId == userId).ToList();
            }
        }

        /// <summary>
        /// Met à jour le token d'une session
        /// </summary>
        public void UpdateSessionToken(string clientId, string newToken)
        {
            lock (_lockObject)
            {
                if (_sessions.TryGetValue(clientId, out var session))
                {
                    session.AuthToken = newToken;
                    session.LastActivityAt = DateTime.UtcNow;
                }
            }
        }
    }
}
