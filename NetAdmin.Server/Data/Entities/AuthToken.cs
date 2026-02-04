using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetAdmin.Server.Data.Entities
{
    public class AuthToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty; // JWT Token

        public string RefreshToken { get; set; } = string.Empty; // Pour renouveler le token

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsActive => !IsExpired && !IsRevoked;

        public string IpAddress { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;

        // Relation
        public User User { get; set; } = null!;
    }
}
