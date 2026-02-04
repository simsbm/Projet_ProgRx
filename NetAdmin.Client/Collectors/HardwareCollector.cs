using System.Diagnostics;
using NetAdmin.Shared.Models;

namespace NetAdmin.Client.Collectors
{
    public class HardwareCollector
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private readonly string _machineName;

        public HardwareCollector()
        {
            // Initialisation des compteurs Windows standard
            // Attention : Cela fonctionne uniquement sous Windows
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            _machineName = Environment.MachineName;

            // Le premier appel à NextValue() retourne toujours 0, il faut l'initialiser
            _cpuCounter.NextValue();
            _ramCounter.NextValue();
        }

        public HardwareInfo Collect()
        {
            // Récupération des valeurs
            float cpu = _cpuCounter.NextValue();
            float ram = _ramCounter.NextValue();

            return new HardwareInfo
            {
                CpuUsage = Math.Round(cpu, 1),
                RamAvailable = ram,
                RamTotal = GetTotalMemoryInMb(), // Voir méthode helper ci-dessous
                MachineName = _machineName,
                OSVersion = Environment.OSVersion.ToString()
            };
        }

        // Helper pour obtenir la RAM totale (approche simple via GC ou Info système)
        private double GetTotalMemoryInMb()
        {
            // Pour un projet académique, on peut simuler ou utiliser une API plus complexe.
            // Ici, une valeur statique ou une API WMI serait mieux, mais restons simples pour l'instant.
            // GC.GetGCMemoryInfo().TotalAvailableMemoryBytes est disponible en .NET récents
            return Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024d / 1024d, 0); 
        }
    }
}