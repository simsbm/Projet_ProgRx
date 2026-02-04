using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetAdmin.Server.Data.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int? UserId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Action { get; set; } = string.Empty;      // Ex: "KILL_PROCESS"
        public string TargetMachine { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;    // Ex: "Admin" (pour backward compatibility)
        public string Details { get; set; } = string.Empty;     // Ex: "Process ID 4502 (Notepad)"
        public bool Success { get; set; }

        // Relation
        public User User { get; set; } = null!;
    }
}