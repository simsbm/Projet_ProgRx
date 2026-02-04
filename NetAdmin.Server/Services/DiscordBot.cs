using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetAdmin.Server.Services
{
    public sealed class DiscordNotifier
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly bool _enabled;
        private readonly string _webhookUrl;
        private readonly string _username;
        private readonly string _avatarUrl;
        private readonly string _serverName;
        private readonly Action<string>? _logger;

        public DiscordNotifier(Action<string>? logger = null)
        {
            _logger = logger;

            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    _enabled = false;
                    _webhookUrl = string.Empty;
                    _username = "NetAdmin Pro";
                    _avatarUrl = string.Empty;
                    _serverName = Environment.MachineName;
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                var discord = config?.Discord ?? new DiscordConfig();

                _enabled = discord.Enabled;
                _webhookUrl = discord.WebhookUrl ?? string.Empty;
                _username = string.IsNullOrWhiteSpace(discord.Username) ? "NetAdmin Pro" : discord.Username;
                _avatarUrl = discord.AvatarUrl ?? string.Empty;
                _serverName = string.IsNullOrWhiteSpace(discord.ServerName) ? Environment.MachineName : discord.ServerName;
            }
            catch (Exception ex)
            {
                _enabled = false;
                _webhookUrl = string.Empty;
                _username = "NetAdmin Pro";
                _avatarUrl = string.Empty;
                _serverName = Environment.MachineName;
                _logger?.Invoke($"[DISCORD] Erreur configuration: {ex.Message}");
            }
        }

        public Task SendAsync(string title, string message, int color = 0x5865F2)
        {
            if (!_enabled || string.IsNullOrWhiteSpace(_webhookUrl))
            {
                return Task.CompletedTask;
            }

            return SendInternalAsync(title, message, color);
        }

        private async Task SendInternalAsync(string title, string message, int color)
        {
            try
            {
                var payload = new
                {
                    username = _username,
                    avatar_url = string.IsNullOrWhiteSpace(_avatarUrl) ? null : _avatarUrl,
                    embeds = new[]
                    {
                        new
                        {
                            title = title,
                            description = message,
                            color = color,
                            timestamp = DateTime.UtcNow.ToString("o"),
                            footer = new { text = $"Serveur: {_serverName}" }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await Http.PostAsync(_webhookUrl, content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.Invoke($"[DISCORD] Erreur HTTP: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[DISCORD] Erreur envoi: {ex.Message}");
            }
        }

        private sealed class AppConfig
        {
            public DiscordConfig? Discord { get; set; }
        }

        private sealed class DiscordConfig
        {
            public bool Enabled { get; set; } = false;
            public string? WebhookUrl { get; set; }
            public string? Username { get; set; }
            public string? AvatarUrl { get; set; }
            public string? ServerName { get; set; }
        }
    }
}
