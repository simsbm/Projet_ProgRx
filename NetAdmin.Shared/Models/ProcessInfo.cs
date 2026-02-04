namespace NetAdmin.Shared.Models
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; } // En MB
        public bool IsCritical { get; set; }    // Pour l'affichage UI
    }
}