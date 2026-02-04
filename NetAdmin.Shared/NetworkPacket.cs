using System.Text.Json;

namespace NetAdmin.Shared
{
    // Types de messages possibles
    public enum PacketType
    {
        // Authentification
        Login,          // Requête de connexion
        LoginResponse,  // Réponse de connexion
        Logout,         // Déconnexion
        RefreshToken,   // Renouvellement de token
        
        // Enregistrement et suivi
        Register,       // Le client se présente
        Heartbeat,      // "Je suis vivant"
        
        // Données
        SystemInfo,     // CPU/RAM data
        ProcessList,    // Réponse liste processus
        
        // Commandes et alertes
        Command,        // Ordre du serveur (Kill, etc.)
        Alert           // Alerte (Seuil dépassé)
    }

    // Le conteneur qui voyage sur le réseau
    public class NetworkPacket
    {
        public PacketType Type { get; set; }
        public string SenderId { get; set; } = string.Empty; // MAC ou Hostname
        public DateTime Timestamp { get; set; }
        public string PayloadJson { get; set; } = string.Empty; // Données variables
        
        // Token d'authentification
        public string AuthToken { get; set; } = string.Empty; // JWT Token pour les requêtes authentifiées
        public string ClientId { get; set; } = string.Empty; // ID unique du client

        // Méthodes utilitaires pour simplifier la vie
        public static NetworkPacket Create<T>(PacketType type, string sender, T data)
        {
            return new NetworkPacket
            {
                Type = type,
                SenderId = sender,
                Timestamp = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(data)
            };
        }

        public static NetworkPacket CreateAuthenticated<T>(PacketType type, string sender, T data, string token)
        {
            var packet = Create(type, sender, data);
            packet.AuthToken = token;
            return packet;
        }

        public T DeserializePayload<T>()
        {
            return JsonSerializer.Deserialize<T>(PayloadJson ?? "{}") ?? throw new InvalidOperationException("Impossible de désérialiser le payload");
        }
    }
}