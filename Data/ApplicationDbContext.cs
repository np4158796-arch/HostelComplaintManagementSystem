using System;
using HostelComplaintManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HostelComplaintManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hostel> Hostels => Set<Hostel>();
        public DbSet<HostelBlock> HostelBlocks => Set<HostelBlock>();
        public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();
        public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintAssignment> ComplaintAssignments => Set<ComplaintAssignment>();
        public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();
        public DbSet<ComplaintComment> ComplaintComments => Set<ComplaintComment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<EmailNotificationLog> EmailNotificationLogs => Set<EmailNotificationLog>();
        public DbSet<HostelPayment> HostelPayments => Set<HostelPayment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Unique Index for Complaint Reference
            builder.Entity<Complaint>()
                .HasIndex(c => c.ReferenceNumber)
                .IsUnique();

            builder.Entity<Complaint>()
                .HasIndex(c => c.Status);

            builder.Entity<Complaint>()
                .HasIndex(c => c.CreatedAt);

            // Configure Relationships to avoid multiple cascade deletion paths in SQL Server
            builder.Entity<Complaint>()
                .HasOne(c => c.Student)
                .WithMany(u => u.SubmittedComplaints)
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.AssignedStaff)
                .WithMany(u => u.AssignedComplaints)
                .HasForeignKey(c => c.AssignedStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.Category)
                .WithMany(cat => cat.Complaints)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.HostelBlock)
                .WithMany(b => b.Complaints)
                .HasForeignKey(c => c.BlockId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ComplaintAssignment>()
                .HasOne(ca => ca.AssignedStaff)
                .WithMany()
                .HasForeignKey(ca => ca.AssignedStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComplaintAssignment>()
                .HasOne(ca => ca.AssignedBy)
                .WithMany()
                .HasForeignKey(ca => ca.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComplaintStatusHistory>()
                .HasOne(sh => sh.ChangedBy)
                .WithMany()
                .HasForeignKey(sh => sh.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComplaintComment>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasOne(n => n.Complaint)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.ComplaintId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<HostelPayment>()
                .HasOne(p => p.Student)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HostelPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}
