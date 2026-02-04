using System;
using System.Collections.Generic;
using System.Windows;
using NetAdmin.Shared;
using NetAdmin.Shared.Models;
using NetAdmin.Server.Services;
using System.Threading.Tasks;

namespace NetAdmin.Server
{
    public partial class ProcessWindow : Window
    {
        private string _targetId;
        private TcpServer _server;

        public ProcessWindow(string machineName, TcpServer server)
        {
            InitializeComponent();
            _targetId = machineName;
            _server = server;
            TxtTargetMachine.Text = $"Machine : {machineName}";
            SetCommandStatus("Pret", "#666666");
            
            _server.OnPacketReceived += async (packet) => await HandleServerResponse(packet);
            
            Log($"ProcessWindow ouvert pour {machineName}");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Charger les processus après que la fenêtre soit complètement chargée
            _ = Task.Delay(300).ContinueWith(_ => Dispatcher.Invoke(() => RefreshProcessList()));
        }

        private async Task RefreshProcessList()
        {
            Log("⟳ Chargement des processus...");
            SetCommandStatus("Chargement des processus...", "#666666");
            try
            {
                await _server.SendToClient(_targetId, new NetworkPacket 
                { 
                    Type = NetAdmin.Shared.PacketType.Command, 
                    PayloadJson = "GET_PROCESSES",
                    SenderId = "SERVER",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Log($"❌ Erreur lors du chargement: {ex.Message}");
                SetCommandStatus($"Erreur: {ex.Message}", "#B00020");
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LstSessionLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            });
        }

        private async Task HandleServerResponse(NetworkPacket packet)
        {
            Log($"📨 Paquet reçu - Type: {packet.Type}, SenderId: {packet.SenderId}, Cible: {_targetId}");
            
            if (packet.SenderId != _targetId) 
            {
                Log($"⚠️ SenderId mismatch: reçu '{packet.SenderId}', attendu '{_targetId}'");
                return;
            }

            switch (packet.Type)
            {
                case NetAdmin.Shared.PacketType.ProcessList:
                    var list = packet.DeserializePayload<List<ProcessInfo>>();
                    Dispatcher.Invoke(() =>
                    {
                        GridProcess.ItemsSource = list;
                        Log($"✓ {list.Count} processus chargés");
                        SetCommandStatus($"{list.Count} processus chargés", "#0B8A3B");
                    });
                    break;

                case NetAdmin.Shared.PacketType.Alert:
                    try
                    {
                        var infoMessage = TryExtractInfo(packet.PayloadJson);
                        if (string.IsNullOrWhiteSpace(infoMessage))
                        {
                            infoMessage = packet.PayloadJson;
                        }

                        Log($"ℹ️ {infoMessage}");
                        SetCommandStatus(infoMessage, "#2980B9");
                    }
                    catch (Exception ex)
                    {
                        Log($"❌ Erreur traitement Alert: {ex.Message}");
                        SetCommandStatus($"Erreur: {ex.Message}", "#B00020");
                    }
                    break;
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshProcessList();
        }

        private async void BtnKill_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var process = btn?.DataContext as ProcessInfo;

            if (process == null) return;

            if (process.IsCritical)
            {
                MessageBox.Show($"⚠ Impossible de terminer {process.Name} - Processus critique!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Êtes-vous sûr de vouloir terminer {process.Name} (PID: {process.Id}) ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Log($"🔴 Tentative de terminer {process.Name}...");
                SetCommandStatus($"Demande d'arrêt PID {process.Id} envoyée...", "#666666");
                await _server.SendToClient(_targetId, new NetworkPacket 
                { 
                    Type = NetAdmin.Shared.PacketType.Command, 
                    PayloadJson = $"KILL:{process.Id}",
                    SenderId = "SERVER",
                    Timestamp = DateTime.UtcNow
                });
                
                await Task.Delay(1500);
                // Refresh the list after killing
                await RefreshProcessList();
            }
        }

        private async void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Redémarrer {_targetId} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Log("🔄 Redémarrage en cours...");
                SetCommandStatus("Demande de redémarrage envoyée...", "#666666");
                await _server.SendToClient(_targetId, new NetworkPacket 
                { 
                    Type = NetAdmin.Shared.PacketType.Command, 
                    PayloadJson = "RESTART",
                    SenderId = "SERVER",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        private async void BtnShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Arrêter {_targetId} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Log("🛑 Arrêt en cours...");
                SetCommandStatus("Demande d'arrêt envoyée...", "#666666");
                await _server.SendToClient(_targetId, new NetworkPacket 
                { 
                    Type = NetAdmin.Shared.PacketType.Command, 
                    PayloadJson = "SHUTDOWN",
                    SenderId = "SERVER",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        private async void BtnExecuteCommand_Click(object sender, RoutedEventArgs e)
        {
            var input = CmdTextBox?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Entrez une commande à exécuter.", "Commande", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Log($"⚡ Exécution: {input}");
            SetCommandStatus("Commande envoyée...", "#666666");
            await _server.SendToClient(_targetId, new NetworkPacket 
            { 
                Type = NetAdmin.Shared.PacketType.Command, 
                PayloadJson = $"EXECUTE:{input}",
                SenderId = "SERVER",
                Timestamp = DateTime.UtcNow
            });
        }

        private async void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            Log("📋 Demande de liste des services...");
            SetCommandStatus("Demande de services envoyée...", "#666666");
            await _server.SendToClient(_targetId, new NetworkPacket 
            { 
                Type = NetAdmin.Shared.PacketType.Command, 
                PayloadJson = "GET_SERVICES",
                SenderId = "SERVER",
                Timestamp = DateTime.UtcNow
            });
        }

        private async void BtnSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            Log("ℹ️ Demande d'infos système...");
            SetCommandStatus("Demande d'infos système envoyée...", "#666666");
            await _server.SendToClient(_targetId, new NetworkPacket 
            { 
                Type = NetAdmin.Shared.PacketType.Command, 
                PayloadJson = "GET_SYSTEMINFO",
                SenderId = "SERVER",
                Timestamp = DateTime.UtcNow
            });
        }

        private void SetCommandStatus(string message, string colorHex)
        {
            Dispatcher.Invoke(() =>
            {
                TxtCommandStatus.Text = message;
                TxtCommandStatus.Foreground =
                    (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(colorHex);
            });
        }

        private static string TryExtractInfo(string payloadJson)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("Info", out var info))
                {
                    return info.GetString() ?? payloadJson;
                }
            }
            catch
            {
                // Ignore JSON errors and fallback to raw payload
            }
            return payloadJson;
        }
    }
}
