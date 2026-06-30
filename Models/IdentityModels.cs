using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.UI;

namespace SmartRecyclingRewardsSystem.Models
{
    // ── ApplicationUser ─────────────────────────────────────────────────
    // Extends the default IdentityUser with our custom fields
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string FullName { get { return FirstName + " " + LastName; } }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        // "Admin", "Resident", or "CollectionOfficer"
        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        [Display(Name = "Date Joined")]
        public DateTime DateJoined { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Points Balance")]
        public int PointsBalance { get; set; }

        [Display(Name = "Receive Email Notifications")]
        public bool ReceiveEmailNotifications { get; set; }

        [Display(Name = "Receive SMS Notifications")]
        public bool ReceiveSmsNotifications { get; set; }

        // Navigation properties
        public virtual ICollection<RecyclingSubmission> RecyclingSubmissions { get; set; }
        public virtual ICollection<RecyclingSubmission> VerifiedSubmissions { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<CollectionEventRegistration> EventRegistrations { get; set; }
        public virtual ICollection<DropOffPoint> AssignedDropOffPoints { get; set; }

        public ApplicationUser()
        {
            DateJoined = DateTime.Now;
            IsActive = true;
            PointsBalance = 0;
            ReceiveEmailNotifications = true;
            ReceiveSmsNotifications = false;
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    // ── ApplicationDbContext ────────────────────────────────────────────
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        // ── App DbSets ───────────────────────────────────────────────
        public DbSet<MaterialType> MaterialTypes { get; set; }
        public DbSet<DropOffPoint> DropOffPoints { get; set; }
        public DbSet<RecyclingSubmission> RecyclingSubmissions { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CollectionEvent> CollectionEvents { get; set; }
        public DbSet<CollectionEventRegistration> CollectionEventRegistrations { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RecyclingSubmission>()
                .HasRequired(s => s.Resident)
                .WithMany(u => u.RecyclingSubmissions)
                .HasForeignKey(s => s.ResidentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<RecyclingSubmission>()
                .HasOptional(s => s.VerifiedByOfficer)
                .WithMany(u => u.VerifiedSubmissions)
                .HasForeignKey(s => s.VerifiedByOfficerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PointTransaction>()
                .HasRequired(t => t.User)
                .WithMany(u => u.PointTransactions)
                .HasForeignKey(t => t.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PointTransaction>()
                .HasOptional(t => t.RecyclingSubmission)
                .WithMany()
                .HasForeignKey(t => t.RecyclingSubmissionId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasOptional(n => n.RecyclingSubmission)
                .WithMany()
                .HasForeignKey(n => n.RecyclingSubmissionId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DropOffPoint>()
                .HasOptional(d => d.AssignedOfficer)
                .WithMany(u => u.AssignedDropOffPoints)
                .HasForeignKey(d => d.AssignedOfficerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CollectionEventRegistration>()
                .HasRequired(r => r.Resident)
                .WithMany(u => u.EventRegistrations)
                .HasForeignKey(r => r.ResidentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CollectionEventRegistration>()
                .HasIndex(r => new { r.ResidentId, r.CollectionEventId })
                .IsUnique();

            modelBuilder.Entity<UserBadge>()
                .HasIndex(b => new { b.UserId, b.BadgeId })
                .IsUnique();

            modelBuilder.Entity<MaterialType>()
                .Property(m => m.PointsPerKg).HasPrecision(10, 2);
            modelBuilder.Entity<MaterialType>()
                .Property(m => m.CO2SavingPerKg).HasPrecision(10, 4);
            modelBuilder.Entity<RecyclingSubmission>()
                .Property(s => s.WeightKg).HasPrecision(10, 2);
            modelBuilder.Entity<RecyclingSubmission>()
                .Property(s => s.CO2SavedKg).HasPrecision(10, 4);
        }
    }
}
