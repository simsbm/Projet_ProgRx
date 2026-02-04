using System.Windows;
using System.Windows.Threading;
using NetAdmin.Server.Services;
using NetAdmin.Shared;
using NetAdmin.Shared.Models;
using System.Collections.ObjectModel;
using NetAdmin.Server.Data.Entities;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using NetAdmin.Server.Data;

namespace NetAdmin.Server
{
    public partial class MainWindow : Window
    {
        private TcpServer _server;
        private DatabaseService _dbService;
        private DispatcherTimer _refreshTimer;
        private DiscordNotifier _discord;

        // Collections pour les graphiques
        private ChartValues<double> _cpuValues = new ChartValues<double>();
        private ChartValues<double> _ramValues = new ChartValues<double>();
        private Dictionary<string, ChartValues<double>> _clientCpuData = new();
        private Dictionary<string, ChartValues<double>> _clientRamData = new();

        public ObservableCollection<ClientHost> ConnectedClients { get; set; } = new ObservableCollection<ClientHost>();

        public MainWindow()
        {
            InitializeComponent();
            
            GridClients.ItemsSource = ConnectedClients;

            _dbService = new DatabaseService();
            _dbService.EnsureDatabaseCreated();
            
            // Initialiser les graphiques
            InitializeCharts();

            _discord = new DiscordNotifier(Server_OnLog);

            _server = new TcpServer(5000);
            _server.OnLog += Server_OnLog;
            _server.OnPacketReceived += Server_OnPacketReceived;
            _server.OnClientConnected += (ip) =>
            {
                Server_OnLog($"UI: Client connecté {ip}");
                _ = _discord.SendAsync("Client connecté", $"IP: {ip}", 0x2ECC71);
            };

            // Configuration du Timer (Toutes les 3 secondes)
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(3);
            _refreshTimer.Tick += (s, e) => { _ = RefreshUIAsync(); };
            _refreshTimer.Start();

            _server.Start(); 
            _ = _discord.SendAsync("Serveur démarré", "NetAdmin Pro est en ligne.", 0x3498DB);
        }

        private void InitializeCharts()
        {
            // Initialiser le graphique CPU
            var cpuSeries = new LineSeries
            {
                Title = "CPU Moyen",
                Values = _cpuValues,
                Stroke = System.Windows.Media.Brushes.OrangeRed,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointForeground = System.Windows.Media.Brushes.OrangeRed
            };

            CpuChart.Series = new SeriesCollection { cpuSeries };

            // Initialiser le graphique RAM
            var ramSeries = new LineSeries
            {
                Title = "RAM Moyenne",
                Values = _ramValues,
                Stroke = System.Windows.Media.Brushes.Green,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointForeground = System.Windows.Media.Brushes.Green
            };

            RamChart.Series = new SeriesCollection { ramSeries };
        }

        // Mission : Récupérer les données fraîches de la BDD et mettre à jour l'UI
        private async Task RefreshUIAsync()
        {
            // 1. Récupérer les clients depuis la BDD
            var clients = _dbService.GetAllClients();

            // 2. Mettre à jour la liste visuelle
            ConnectedClients.Clear();
            foreach (var client in clients)
            {
                ConnectedClients.Add(client);
            }

            // 3. Calculer les statistiques globales et mettre à jour les graphiques
            if (clients.Any())
            {
                UpdateGlobalStats(clients);
                await UpdateChartsAsync(clients);
            }
        }

        private async Task UpdateChartsAsync(List<ClientHost> clients)
        {
            try
            {
                // Récupérer les dernières métriques pour chaque client
                using (var context = new AppDbContext())
                {
                    // Récupérer les 100 dernières métriques (toutes les sources)
                    var allMetrics = context.MetricLogs
                        .OrderByDescending(m => m.Timestamp)
                        .Take(100)
                        .ToList();

                    if (!allMetrics.Any())
                    {
                        Server_OnLog("[CHARTS] Pas de métriques trouvées");
                        return;
                    }

                    // Calculer les moyennes
                    double avgCpu = allMetrics.Average(m => m.CpuUsage);
                    double avgRam = allMetrics.Average(m => m.RamAvailable) / 1024; // Convertir MB en GB

                    Server_OnLog($"[CHARTS] Ajout - CPU: {avgCpu:F1}% | RAM: {avgRam:F2}GB (Count: {allMetrics.Count})");

                    // Ajouter aux graphiques
                    Dispatcher.Invoke(() =>
                    {
                        _cpuValues.Add(avgCpu);
                        _ramValues.Add(avgRam);

                        // Limiter à 20 points pour éviter un graphique trop chargé
                        if (_cpuValues.Count > 20)
                            _cpuValues.RemoveAt(0);
                        if (_ramValues.Count > 20)
                            _ramValues.RemoveAt(0);
                    });
                }
            }
            catch (Exception ex)
            {
                Server_OnLog($"[ERROR CHARTS] {ex.Message}");
            }
        }

        private void UpdateGlobalStats(List<ClientHost> clients)
        {
            // Mise à jour des TextBlock définis dans le XAML précédent
            int onlineCount = clients.Count(c => c.IsOnline);
            TxtAvgCpu.Text = $"{onlineCount} Actif(s)"; 
            
            // On peut aussi afficher la dernière IP ou le nom du dernier client
            var lastClient = clients.OrderByDescending(c => c.LastSeen).FirstOrDefault();
            if (lastClient != null)
            {
                TxtAvgRam.Text = lastClient.MachineName;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _server.Start();
        }

        // Callback quand un log arrive (Thread Réseau) -> Dispatch vers UI
        private void Server_OnLog(string message)
        {
            Console.WriteLine(message);
            Dispatcher.Invoke(() => 
            {
                LstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                System.Diagnostics.Debug.WriteLine(message);
            });
        }

        // Callback quand un paquet JSON arrive
        private async void Server_OnPacketReceived(NetworkPacket packet)
        {
            switch (packet.Type)
            {
                case NetAdmin.Shared.PacketType.Register:
                    Server_OnLog($"[REGISTER] Client: {packet.SenderId}");
                    await _dbService.UpdateClientStatusAsync(packet.SenderId, "0.0.0.0", "Windows");
                    _ = _discord.SendAsync("Client enregistré", $"ID: {packet.SenderId}", 0xF1C40F);
                    break;

                case NetAdmin.Shared.PacketType.SystemInfo:
                    var metrics = packet.DeserializePayload<HardwareInfo>();
                    if (metrics != null)
                    {
                        Server_OnLog($"[METRICS] {packet.SenderId} - CPU: {metrics.CpuUsage}% | RAM: {metrics.RamAvailable}MB");
                        await _dbService.SaveMetricsAsync(packet.SenderId, metrics);
                    }
                    else
                    {
                        Server_OnLog($"[ERROR] Metrics NULL pour {packet.SenderId}");
                    }
                    break;
            }
        }

        private void BtnManageClient_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                var client = btn?.DataContext as ClientHost;

                if (client == null)
                {
                    MessageBox.Show("Erreur: Client non sélectionné", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (client.MachineName == null)
                {
                    MessageBox.Show("Erreur: MachineName vide", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Server_OnLog($"[UI] Ouverture ProcessWindow pour {client.MachineName}");
                var processWindow = new ProcessWindow(client.MachineName, _server);
                processWindow.Owner = this;
                processWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture: {ex.Message}\n{ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Server_OnLog($"[ERROR] BtnManageClient_Click: {ex}");
            }
        }

        private void NavOverview_Click(object sender, RoutedEventArgs e)
        {
            ScrollToSection(SectionOverview);
        }

        private void NavClients_Click(object sender, RoutedEventArgs e)
        {
            ScrollToSection(SectionClients);
        }

        private void NavAudit_Click(object sender, RoutedEventArgs e)
        {
            ScrollToSection(SectionAudit);
        }

        private void ScrollToSection(FrameworkElement section)
        {
            if (section == null || MainScrollViewer == null)
            {
                return;
            }

            section.BringIntoView();

            MainScrollViewer.Dispatcher.InvokeAsync(() =>
            {
                var transform = section.TransformToAncestor(MainScrollViewer);
                var point = transform.Transform(new Point(0, 0));
                MainScrollViewer.ScrollToVerticalOffset(point.Y + MainScrollViewer.VerticalOffset);
            }, DispatcherPriority.Background);
        }

        private void LogToUI(string msg)
        {
            Dispatcher.Invoke(() => 
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            });
        }
    }
}
