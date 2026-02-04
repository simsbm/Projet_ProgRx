using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetAdmin.Shared; // Référence à ton projet Shared

namespace NetAdmin.Server.Services
{
    public class TcpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts = null!;
        
        // Dictionnaire Thread-Safe pour stocker les clients (IP:Port -> Client)
        // La clé sera l'identifiant unique du client (ex: son adresse IP ou un ID envoyé au login)
        private ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();

        // Mapping from MachineName to clientEndPoint (IP:Port)
        private readonly ConcurrentDictionary<string, string> _machineNameToEndPoint = new();

        // Événements pour prévenir l'UI
        public event Action<string> OnLog = delegate { };
        public event Action<string> OnClientConnected = delegate { };
        public event Action<string> OnClientDisconnected = delegate { };
        public event Action<NetworkPacket> OnPacketReceived = delegate { };

        public bool IsRunning { get; private set; }

        public TcpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _listener.Start();
            IsRunning = true;
            Log($"Serveur démarré sur le port {_listener.LocalEndpoint}");

            // Lancer la boucle d'acceptation dans une tâche séparée pour ne pas bloquer l'UI
            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
            IsRunning = false;
            
            // Fermer proprement tous les clients
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            Log("Serveur arrêté.");
        }

        // Boucle principale qui attend les connexions
        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Attend un client de manière asynchrone
                    TcpClient client = await _listener.AcceptTcpClientAsync(token);
                    
                    // Dès qu'un client arrive, on lance sa gestion dans sa propre tâche
                    // On ne l'attend pas (fire and forget), pour revenir écouter immédiatement
                    _ = Task.Run(() => HandleClientAsync(client, token));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log($"Erreur d'acceptation : {ex.Message}");
                }
            }
        }

        // Gère la vie d'un client spécifique
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string clientEndPoint = client.Client.RemoteEndPoint.ToString();
            Log($"Nouveau client connecté : {clientEndPoint}");
            OnClientConnected?.Invoke(clientEndPoint);

            // On ajoute temporairement le client avec son IP comme ID
            // Idéalement, on mettrait à jour cette clé après le packet "Register" contenant le nom de la machine
            _clients.TryAdd(clientEndPoint, client);

            NetworkStream stream = client.GetStream();

            try
            {
                while (!token.IsCancellationRequested && client.Connected)
                {
                    // --- PROTOCOLE DE FRAMING (Longueur + Contenu) ---

                    // 1. Lire les 4 premiers octets (Taille du message)
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await ReadExactAsync(stream, lengthBuffer, 4, token);
                    
                    if (bytesRead == 0) break; // Le client a fermé la connexion proprement

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 2. Lire le contenu exact (Payload JSON)
                    byte[] messageBuffer = new byte[messageLength];
                    await ReadExactAsync(stream, messageBuffer, messageLength, token);

                    // 3. Désérialiser et lever l'événement
                    string json = Encoding.UTF8.GetString(messageBuffer);
                    
                    try 
                    {
                        var packet = JsonSerializer.Deserialize<NetworkPacket>(json);
                        
                        // Si le client s'enregistre, on enregistre le mapping MachineName -> EndPoint
                        if (packet.Type == PacketType.Register)
                        {
                            var registerInfo = packet.DeserializePayload<dynamic>();
                            string machineName = packet.SenderId;
                            _machineNameToEndPoint[machineName] = clientEndPoint;
                            Log($"✓ Client enregistré: {machineName} -> {clientEndPoint}");
                        }
                        
                        // Notifier l'UI qu'un paquet est arrivé
                        OnPacketReceived?.Invoke(packet);
                    }
                    catch (JsonException)
                    {
                        Log($"Erreur JSON reçu de {clientEndPoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Erreur avec le client {clientEndPoint} : {ex.Message}");
            }
            finally
            {
                // Nettoyage lors de la déconnexion
                _clients.TryRemove(clientEndPoint, out _);
                client.Close();
                OnClientDisconnected?.Invoke(clientEndPoint);
                Log($"Client déconnecté : {clientEndPoint}");
            }
        }

        // Méthode utilitaire pour envoyer une commande à un client spécifique
        public async Task SendToClient(string clientId, NetworkPacket packet)
        {
            // clientId can be either:
            // 1. Direct endpoint (IP:Port)
            // 2. MachineName (needs to be looked up)
            
            string endPoint = clientId;
            
            // Si ce n'est pas un endpoint direct, chercher la correspondance
            if (!clientId.Contains(":"))
            {
                if (_machineNameToEndPoint.TryGetValue(clientId, out string mappedEndPoint))
                {
                    endPoint = mappedEndPoint;
                    Log($"Lookup MachineName '{clientId}' -> '{endPoint}'");
                }
                else
                {
                    Log($"❌ Client introuvable : {clientId}");
                    return;
                }
            }
            
            if (_clients.TryGetValue(endPoint, out TcpClient client))
            {
                try
                {
                    string json = JsonSerializer.Serialize(packet);
                    byte[] payload = Encoding.UTF8.GetBytes(json);
                    byte[] length = BitConverter.GetBytes(payload.Length);

                    NetworkStream stream = client.GetStream();
                    
                    // Envoi atomique (Taille + Données)
                    await stream.WriteAsync(length, 0, length.Length);
                    await stream.WriteAsync(payload, 0, payload.Length);
                    Log($"✓ Packet envoyé à {endPoint}");
                }
                catch (Exception ex)
                {
                    Log($"❌ Erreur d'envoi à {endPoint}: {ex.Message}");
                }
            }
            else
            {
                Log($"❌ Client pas connecté : {endPoint}");
            }
        }

        // Helper pour lire exactement N octets (Vital pour TCP !)
        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
                if (read == 0) return 0; // Socket fermé
                totalRead += read;
            }
            return totalRead;
        }

        private void Log(string message)
        {
            // Invoke pour s'assurer que l'UI récupère le log
            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}