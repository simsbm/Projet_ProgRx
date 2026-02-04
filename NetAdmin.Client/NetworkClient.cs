using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetAdmin.Shared;
using NetAdmin.Client.Collectors;
using NetAdmin.Shared.Models;

namespace NetAdmin.Client
{
    public class NetworkClient
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private readonly HardwareCollector _collector;
        private readonly ProcessCollector _processCollector; // Nouveau
        private bool _isRunning;

        public NetworkClient(string ip, int port)
        {
            _serverIp = ip;
            _serverPort = port;
            _collector = new HardwareCollector();
            _processCollector = new ProcessCollector(); // Initialisation
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            Console.WriteLine($"[Agent] Démarrage... Cible: {_serverIp}:{_serverPort}");

            while (_isRunning)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        Console.WriteLine("[Agent] Tentative de connexion...");
                        await client.ConnectAsync(_serverIp, _serverPort);
                        Console.WriteLine("[Agent] Connecté au serveur !");

                        using (NetworkStream stream = client.GetStream())
                        {
                            // 1. Enregistrement
                            await SendPacketAsync(stream, PacketType.Register, new { Name = Environment.MachineName });

                            // 2. Boucle de communication bidirectionnelle
                            while (client.Connected)
                            {
                                // --- PARTIE ENVOI (Metrics) ---
                                var metrics = _collector.Collect();
                                var packet = NetworkPacket.Create(PacketType.SystemInfo, Environment.MachineName, metrics);
                                await SendPacketAsync(stream, packet);

                                // --- PARTIE RÉCEPTION (Commandes) ---
                                // On vérifie si le serveur a envoyé quelque chose (sans bloquer)
                                if (stream.DataAvailable)
                                {
                                    await HandleIncomingCommand(stream);
                                }

                                await Task.Delay(2000); // Intervalle de monitoring
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Agent] Erreur ou Déconnexion : {ex.Message}");
                    await Task.Delay(5000);
                }
            }
        }

        private async Task HandleIncomingCommand(NetworkStream stream)
        {
            try
            {
                // Lecture de la taille (Framing)
                byte[] lengthBuffer = new byte[4];
                await stream.ReadAsync(lengthBuffer, 0, 4);
                int length = BitConverter.ToInt32(lengthBuffer, 0);

                // Lecture du JSON
                byte[] dataBuffer = new byte[length];
                await stream.ReadAsync(dataBuffer, 0, length);
                string json = Encoding.UTF8.GetString(dataBuffer);

                var packet = JsonSerializer.Deserialize<NetworkPacket>(json);

                if (packet?.Type == PacketType.Command)
                {
                    string command = packet.PayloadJson;
                    Console.WriteLine($"[Agent] Commande reçue : {command}");

                    if (command.StartsWith("KILL:"))
                    {
                        try
                        {
                            int pid = int.Parse(command.Split(':')[1]);
                            Console.WriteLine($"[Agent] Tentative de terminer PID {pid}...");
                            bool success = _processCollector.KillProcess(pid);
                            Console.WriteLine($"[Agent] KillProcess retourné: {success}");
                            await SendPacketAsync(stream, PacketType.Alert, new { Info = $"Kill {pid}: {(success ? "SUCCESS" : "FAILED")}" });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Agent] ❌ Erreur KILL: {ex.Message}");
                            await SendPacketAsync(stream, PacketType.Alert, new { Info = $"Kill failed: {ex.Message}" });
                        }
                    }
                    else if (command == "GET_PROCESSES")
                    {
                        var list = _processCollector.GetRunningProcesses();
                        await SendPacketAsync(stream, PacketType.ProcessList, list);
                    }
                    else if (command == "RESTART")
                    {
                        Console.WriteLine("[Agent] RESTART commandé - redémarrage en 30 secondes...");
                        System.Diagnostics.Process.Start("shutdown.exe", "-r -t 30 -c \"Redémarrage commandé par administrateur\"");
                        await SendPacketAsync(stream, PacketType.Alert, new { Info = "Redémarrage lancé" });
                    }
                    else if (command == "SHUTDOWN")
                    {
                        Console.WriteLine("[Agent] SHUTDOWN commandé - arrêt en 30 secondes...");
                        System.Diagnostics.Process.Start("shutdown.exe", "-s -t 30 -c \"Arrêt commandé par administrateur\"");
                        await SendPacketAsync(stream, PacketType.Alert, new { Info = "Arrêt lancé" });
                    }
                    else if (command.StartsWith("EXECUTE:"))
                    {
                        string cmd = command.Substring(8);
                        Console.WriteLine($"[Agent] Exécution: {cmd}");
                        try
                        {
                            System.Diagnostics.Process.Start("cmd.exe", $"/c {cmd}");
                            await SendPacketAsync(stream, PacketType.Alert, new { Info = $"Commande exécutée: {cmd}" });
                        }
                        catch (Exception ex)
                        {
                            await SendPacketAsync(stream, PacketType.Alert, new { Info = $"Erreur: {ex.Message}" });
                        }
                    }
                    else if (command == "GET_SYSTEMINFO")
                    {
                        var sysInfo = new
                        {
                            ComputerName = Environment.MachineName,
                            OSVersion = Environment.OSVersion.ToString(),
                            ProcessorCount = Environment.ProcessorCount,
                            TotalMemoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024
                        };
                        await SendPacketAsync(stream, PacketType.Alert, sysInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agent] Erreur lecture commande : {ex.Message}");
            }
        }

        public async Task SendPacketAsync(NetworkPacket packet)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(_serverIp, _serverPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        await SendPacketAsync(stream, packet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NetworkClient] Erreur lors de l'envoi du packet: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task SendPacketAsync<T>(NetworkStream stream, PacketType type, T payload)
        {
            var packet = NetworkPacket.Create(type, Environment.MachineName, payload);
            await SendPacketAsync(stream, packet);
        }

        public async Task SendPacketAsync(NetworkStream stream, NetworkPacket packet)
        {
            string json = JsonSerializer.Serialize(packet);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lengthHeader = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(lengthHeader, 0, lengthHeader.Length);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
}