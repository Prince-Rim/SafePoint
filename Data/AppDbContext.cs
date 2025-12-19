using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Models;

namespace SafePoint_IRS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Incident> Incident { get; set; }
        public DbSet<RejectedIncident> RejectedIncidents { get; set; }
        public DbSet<IncidentArchive> IncidentArchives { get; set; }
        public DbSet<UserArchive> UserArchives { get; set; }
        public DbSet<Valid> Valid { get; set; }
        public DbSet<IType> IncidentTypes { get; set; }
        public DbSet<Area> Area { get; set; }
        public DbSet<Moderator> Moderators { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Incident>().ToTable("Incident");
            modelBuilder.Entity<Valid>().ToTable("Valid");
            modelBuilder.Entity<IType>().ToTable("IType");
            modelBuilder.Entity<Area>().ToTable("Area");
            modelBuilder.Entity<Moderator>().ToTable("Moderator");
            modelBuilder.Entity<Admin>().ToTable("Admin");
            modelBuilder.Entity<Comment>().ToTable("Comment");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserArchive>().ToTable("UserArchive");
            modelBuilder.Entity<IncidentArchive>().ToTable("IncidentArchive");
            modelBuilder.Entity<UserBadge>().ToTable("UserBadge");

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.ValidStatus)
                .WithOne(v => v.Incident)
                .HasForeignKey<Valid>(v => v.IncidentID);

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.Area)
                .WithMany(a => a.Incidents)
                .HasForeignKey(i => i.Area_Code)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Moderator>()
                .HasOne(m => m.Area)
                .WithMany(a => a.Moderators)
                .HasForeignKey(m => m.Area_Code)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Incident>()
                .Property(i => i.Latitude)
                .HasColumnType("decimal(10,8)");

            modelBuilder.Entity<Incident>()
                .Property(i => i.Longitude)
                .HasColumnType("decimal(11,8)");

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User) 
                .WithMany(u => u.Comments) 
                .HasForeignKey(c => c.Userid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Admin)
                .WithMany()
                .HasForeignKey(c => c.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Moderator)
                .WithMany()
                .HasForeignKey(c => c.ModId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Incident)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IncidentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RejectedIncident>()
                .Property(r => r.Latitude)
                .HasColumnType("decimal(10,8)");

            modelBuilder.Entity<RejectedIncident>()
                .Property(r => r.Longitude)
                .HasColumnType("decimal(11,8)");

            modelBuilder.Entity<IncidentArchive>()
                .Property(ia => ia.Latitude)
                .HasColumnType("decimal(10,8)");

            modelBuilder.Entity<IncidentArchive>()
                .Property(ia => ia.Longitude)
                .HasColumnType("decimal(11,8)");
        }
    }
}
