using System.Text.Json;
using NetAdmin.Client;

try
{
    Console.Title = "NetAdmin Agent Pro";
    Console.WriteLine("[Agent] Demarrage...");

    // 1) Priorite: arguments CLI -> appsettings.json -> saisie manuelle
    string? serverIp = null;
    int? port = null;

    if (args.Length >= 1)
    {
        serverIp = args[0];
    }
    if (args.Length >= 2 && int.TryParse(args[1], out int parsedPort))
    {
        port = parsedPort;
    }

    // 2) Lire appsettings.json si dispo
    if (string.IsNullOrWhiteSpace(serverIp) || port == null)
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Server", out var server))
                {
                    if (string.IsNullOrWhiteSpace(serverIp) && server.TryGetProperty("Host", out var hostProp))
                    {
                        serverIp = hostProp.GetString();
                    }
                    if (port == null && server.TryGetProperty("Port", out var portProp) && portProp.TryGetInt32(out int p))
                    {
                        port = p;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agent] Erreur lecture appsettings.json: {ex.Message}");
            }
        }
    }

    // 3) Saisie manuelle si toujours vide
    if (string.IsNullOrWhiteSpace(serverIp))
    {
        Console.Write("Adresse du serveur (ex: 192.168.1.10): ");
        serverIp = Console.ReadLine();
    }
    if (port == null)
    {
        Console.Write("Port (ex: 5000): ");
        var input = Console.ReadLine();
        if (int.TryParse(input, out int p))
        {
            port = p;
        }
    }

    if (string.IsNullOrWhiteSpace(serverIp) || port == null)
    {
        Console.WriteLine("[Agent] Configuration serveur invalide. Arret.");
        Console.ReadLine();
        return;
    }

    Console.WriteLine($"[Agent] Serveur cible: {serverIp}:{port}");

    var agent = new NetworkClient(serverIp, port.Value);

    // Lancement bloquant (c'est une application console qui doit rester ouverte)
    await agent.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[ERREUR FATALE] {ex.Message}");
    Console.WriteLine($"[STACK TRACE] {ex.StackTrace}");
    Console.ReadLine(); // Pause pour lire l'erreur
}
