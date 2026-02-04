using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data.Entities;

namespace NetAdmin.Server.Data
{
    public class AppDbContext : DbContext
    {
        // Tables d'authentification
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuthToken> AuthTokens { get; set; } = null!;
        
        // Tables de suivi
        public DbSet<ClientHost> ClientHosts { get; set; } = null!;
        public DbSet<MetricLog> MetricLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configuration de la chaîne de connexion
            // Le fichier netadmin.db sera créé à la racine de l'exécutable
            optionsBuilder.UseSqlite("Data Source=netadmin.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index pour les utilisateurs
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Relation User -> AuthTokens
            modelBuilder.Entity<AuthToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.AuthTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation User -> AuditLogs
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Optimisation : Indexer le MachineName pour des recherches rapides
            modelBuilder.Entity<ClientHost>()
                .HasIndex(c => c.MachineName)
                .IsUnique();
        }
    }
}