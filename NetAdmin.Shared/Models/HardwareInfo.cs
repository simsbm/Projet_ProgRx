namespace NetAdmin.Shared.Models
{
    public class HardwareInfo
    {
        public double CpuUsage { get; set; }
        public double RamAvailable { get; set; } // En MB
        public double RamTotal { get; set; }     // En MB
        public string MachineName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
    }
}