using Microsoft.EntityFrameworkCore;
using MeetingManagement.Models;

namespace MeetingManagement.Data
{
    public class MeetingManagementDbContext : DbContext
    {
        public MeetingManagementDbContext(DbContextOptions<MeetingManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingLog> MeetingLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ProfileImagePath).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Meeting configuration
            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DocumentPath).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Meetings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MeetingLog configuration
            modelBuilder.Entity<MeetingLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DocumentPath).HasMaxLength(255);
                entity.Property(e => e.Operation).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LoggedBy).HasMaxLength(100);
                entity.Property(e => e.LoggedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}