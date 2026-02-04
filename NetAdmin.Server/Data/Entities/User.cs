using System.ComponentModel.DataAnnotations;

namespace NetAdmin.Server.Data.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Hash BCrypt

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Operator;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Relation : Un utilisateur peut avoir plusieurs tokens
        public List<AuthToken> AuthTokens { get; set; } = new List<AuthToken>();

        // Relation : Un utilisateur peut avoir plusieurs logs d'audit
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public enum UserRole
    {
        Administrator = 0,
        Supervisor = 1,
        Operator = 2,
        Viewer = 3
    }
}
