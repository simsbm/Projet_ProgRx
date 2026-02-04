using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data;
using NetAdmin.Server.Data.Entities;
using NetAdmin.Shared.Models;

namespace NetAdmin.Server.Services
{
    public class DatabaseService
    {
        // Initialise la BDD au démarrage
        public void EnsureDatabaseCreated()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
        }

        // Met à jour ou Crée un client (Upsert)
        public async Task UpdateClientStatusAsync(string machineName, string ip, string osVersion)
        {
            using var context = new AppDbContext();
            
            var client = await context.ClientHosts
                .FirstOrDefaultAsync(c => c.MachineName == machineName);

            if (client == null)
            {
                client = new ClientHost 
                { 
                    MachineName = machineName,
                    IsOnline = true
                };
                context.ClientHosts.Add(client);
            }

            client.IpAddress = ip;
            client.OSVersion = osVersion;
            client.LastSeen = DateTime.Now;
            client.IsOnline = true;

            await context.SaveChangesAsync();
        }

        // Sauvegarde les métriques reçues
        public async Task SaveMetricsAsync(string machineName, HardwareInfo info)
        {
            using var context = new AppDbContext();

            // On récupère l'ID du client (nécessaire pour la clé étrangère)
            var client = await context.ClientHosts
                .FirstOrDefaultAsync(c => c.MachineName == machineName);

            if (client != null)
            {
                var log = new MetricLog
                {
                    ClientHostId = client.Id,
                    CpuUsage = info.CpuUsage,
                    RamAvailable = info.RamAvailable,
                    Timestamp = DateTime.Now
                };

                context.MetricLogs.Add(log);
                await context.SaveChangesAsync();
            }
        }

        // Trace une action d'administration (Sécurité)
        public async Task LogActionAsync(string user, string action, string target, string details, bool success)
        {
            using var context = new AppDbContext();
            
            var audit = new AuditLog
            {
                Username = user,
                Action = action,
                TargetMachine = target,
                Details = details,
                Success = success,
                Timestamp = DateTime.Now
            };

            context.AuditLogs.Add(audit);
            await context.SaveChangesAsync();
        }
        
        // Récupère la liste pour l'UI
        public List<ClientHost> GetAllClients()
        {
            using var context = new AppDbContext();
            return context.ClientHosts.ToList();
        }
    }
}