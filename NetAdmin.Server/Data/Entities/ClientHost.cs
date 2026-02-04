using System.ComponentModel.DataAnnotations;

namespace NetAdmin.Server.Data.Entities
{
    public class ClientHost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MachineName { get; set; } = string.Empty; // Identifiant unique (ou MAC address)

        public string IpAddress { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;

        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }

        // Relation : Une machine a plusieurs logs
        public List<MetricLog> Metrics { get; set; } = new List<MetricLog>();
    }
}