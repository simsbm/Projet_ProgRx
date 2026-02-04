using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetAdmin.Server.Data.Entities
{
    public class MetricLog
    {
        [Key]
        public long Id { get; set; }

        public double CpuUsage { get; set; }
        public double RamAvailable { get; set; }
        public DateTime Timestamp { get; set; }

        // Clé étrangère vers ClientHost
        public int ClientHostId { get; set; }
        
        [ForeignKey("ClientHostId")]
        public ClientHost ClientHost { get; set; } = null!;
    }
}