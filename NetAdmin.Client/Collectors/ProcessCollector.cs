using System.Diagnostics;
using NetAdmin.Shared.Models;

namespace NetAdmin.Client.Collectors
{
    public class ProcessCollector
    {
        // Liste noire : On interdit de toucher à ces processus vitaux
        private readonly string[] _criticalProcesses = { "system", "idle", "explorer", "svchost", "wininit", "services" };

        public List<ProcessInfo> GetRunningProcesses()
        {
            var processes = Process.GetProcesses();
            var list = new List<ProcessInfo>();

            foreach (var p in processes)
            {
                try
                {
                    list.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryUsage = Math.Round(p.WorkingSet64 / 1024.0 / 1024.0, 2),
                        IsCritical = _criticalProcesses.Contains(p.ProcessName.ToLower())
                    });
                }
                catch { /* Certains processus système sont inaccessibles */ }
            }
            return list.OrderByDescending(x => x.MemoryUsage).Take(50).ToList();
        }

        public bool KillProcess(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                
                // Vérification de sécurité ultime côté client
                if (_criticalProcesses.Contains(p.ProcessName.ToLower()))
                    return false;

                p.Kill();
                return true;
            }
            catch { return false; }
        }
    }
}