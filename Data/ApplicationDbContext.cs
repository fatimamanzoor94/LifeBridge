using Microsoft.EntityFrameworkCore;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ==================== DBSETS ====================
        public DbSet<User> Users { get; set; }
        public DbSet<DonorProfile> DonorProfiles { get; set; }
        public DbSet<ReceiverProfile> ReceiverProfiles { get; set; }
        public DbSet<HospitalProfile> HospitalProfiles { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }

        // Smart Match Entities
        public DbSet<DonorMatch> DonorMatches { get; set; }
        public DbSet<ContactLog> ContactLogs { get; set; }

        // Notification Entities
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<DonorNotification> DonorNotifications { get; set; }
        public DbSet<ReceiverNotification> ReceiverNotifications { get; set; }
        public DbSet<HospitalNotification> HospitalNotifications { get; set; }  // ✅ ADDED
        public DbSet<AdminAnnouncement> AdminAnnouncements { get; set; }

        // Drafts & Blood Bank
        public DbSet<BloodRequestDraft> BloodRequestDrafts { get; set; }
        public DbSet<BloodInventory> BloodInventory { get; set; }  // ✅ ADDED

        public DbSet<BloodIssueHistory> BloodIssueHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== TABLE MAPPINGS ====================
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<DonorProfile>().ToTable("DonorProfiles");
            modelBuilder.Entity<ReceiverProfile>().ToTable("ReceiverProfiles");
            modelBuilder.Entity<HospitalProfile>().ToTable("HospitalProfiles");
            modelBuilder.Entity<BloodRequest>().ToTable("BloodRequests");
            modelBuilder.Entity<ContactMessage>().ToTable("ContactMessages");
            modelBuilder.Entity<Donation>().ToTable("Donations");
            modelBuilder.Entity<AdminLog>().ToTable("AdminLogs");
            modelBuilder.Entity<EmergencyAlert>().ToTable("EmergencyAlerts");
            modelBuilder.Entity<DonorMatch>().ToTable("DonorMatches");
            modelBuilder.Entity<ContactLog>().ToTable("ContactLogs");
            modelBuilder.Entity<NotificationLog>().ToTable("NotificationLogs");
            modelBuilder.Entity<DonorNotification>().ToTable("DonorNotifications");
            modelBuilder.Entity<ReceiverNotification>().ToTable("ReceiverNotifications");
            modelBuilder.Entity<HospitalNotification>().ToTable("HospitalNotifications");  // ✅ ADDED
            modelBuilder.Entity<AdminAnnouncement>().ToTable("AdminAnnouncements");
            modelBuilder.Entity<BloodRequestDraft>().ToTable("BloodRequestDrafts");
            modelBuilder.Entity<BloodInventory>().ToTable("BloodInventory");  // ✅ ADDED

            // ==================== PRIMARY KEYS ====================
            modelBuilder.Entity<User>().HasKey(e => e.UserId);
            modelBuilder.Entity<DonorProfile>().HasKey(e => e.DonorId);
            modelBuilder.Entity<ReceiverProfile>().HasKey(e => e.ReceiverId);
            modelBuilder.Entity<HospitalProfile>().HasKey(e => e.HospitalId);
            modelBuilder.Entity<BloodRequest>().HasKey(e => e.RequestId);
            modelBuilder.Entity<ContactMessage>().HasKey(e => e.MessageId);
            modelBuilder.Entity<Donation>().HasKey(e => e.DonationId);
            modelBuilder.Entity<AdminLog>().HasKey(e => e.LogId);
            modelBuilder.Entity<EmergencyAlert>().HasKey(e => e.AlertId);
            modelBuilder.Entity<DonorMatch>().HasKey(e => e.MatchId);
            modelBuilder.Entity<ContactLog>().HasKey(e => e.LogId);
            modelBuilder.Entity<NotificationLog>().HasKey(e => e.LogId);
            modelBuilder.Entity<DonorNotification>().HasKey(e => e.NotificationId);
            modelBuilder.Entity<ReceiverNotification>().HasKey(e => e.NotificationId);
            modelBuilder.Entity<HospitalNotification>().HasKey(e => e.NotificationId);  // ✅ ADDED
            modelBuilder.Entity<AdminAnnouncement>().HasKey(e => e.AnnouncementId);
            modelBuilder.Entity<BloodRequestDraft>().HasKey(e => e.DraftId);
            modelBuilder.Entity<BloodInventory>().HasKey(e => e.InventoryId);  // ✅ ADDED

            // ==================== RELATIONSHIPS ====================
            modelBuilder.Entity<DonorProfile>()
                .HasOne(d => d.User).WithOne(u => u.DonorProfile)
                .HasForeignKey<DonorProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReceiverProfile>()
                .HasOne(r => r.User).WithOne(u => u.ReceiverProfile)
                .HasForeignKey<ReceiverProfile>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HospitalProfile>()
                .HasOne(h => h.User).WithOne(u => u.HospitalProfile)
                .HasForeignKey<HospitalProfile>(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BloodRequest>()
                .HasOne(b => b.Receiver).WithMany(u => u.BloodRequests)
                .HasForeignKey(b => b.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            // BloodRequest - Hospital Relationship
            modelBuilder.Entity<BloodRequest>()
                .HasOne(b => b.Hospital)
                .WithMany()
                .HasForeignKey(b => b.HospitalId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Donor).WithMany(u => u.Donations)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AdminLog>()
                .HasOne(a => a.Admin).WithMany(u => u.AdminLogs)
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EmergencyAlert>()
                .HasOne(e => e.BloodRequest)
                .WithMany()
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==================== SMART MATCH RELATIONSHIPS ====================
            modelBuilder.Entity<DonorMatch>()
                .HasOne(dm => dm.BloodRequest)
                .WithMany()
                .HasForeignKey(dm => dm.BloodRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DonorMatch>()
                .HasOne(dm => dm.Donor)
                .WithMany()
                .HasForeignKey(dm => dm.DonorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DonorMatch>()
                .HasOne(dm => dm.Admin)
                .WithMany()
                .HasForeignKey(dm => dm.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            // DonorMatch - Hospital Relationship
            modelBuilder.Entity<DonorMatch>()
                .HasOne(dm => dm.Hospital)
                .WithMany()
                .HasForeignKey(dm => dm.HospitalId)
                .OnDelete(DeleteBehavior.NoAction);

            // ContactLog Relationships
            modelBuilder.Entity<ContactLog>()
                .HasOne(cl => cl.Donor)
                .WithMany()
                .HasForeignKey(cl => cl.DonorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ContactLog>()
                .HasOne(cl => cl.Admin)
                .WithMany()
                .HasForeignKey(cl => cl.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            // HospitalNotification Relationships
            modelBuilder.Entity<HospitalNotification>()
                .HasOne(hn => hn.Hospital)
                .WithMany()
                .HasForeignKey(hn => hn.HospitalId)
                .OnDelete(DeleteBehavior.Cascade);

            // BloodInventory - Hospital Relationship
            modelBuilder.Entity<BloodInventory>()
                .HasOne(bi => bi.Hospital)
                .WithMany()
                .HasForeignKey(bi => bi.HospitalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}