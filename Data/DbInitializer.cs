using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            try
            {
                // Ensure database is created and migrations applied
                await context.Database.MigrateAsync();

                // 1. Seed Roles
                string[] roles = { "Warden", "MaintenanceStaff", "Student" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // 2. Seed Hostel, Blocks, and Rooms
                if (!await context.Hostels.AnyAsync())
                {
                    var hostel = new Hostel
                    {
                        Name = "HostelCare Campus Residences",
                        Code = "HC-MAIN-01",
                        Address = "University Campus, Sector 4, Tech City",
                        TotalRooms = 150,
                        Description = "Premier student residential complex with modern living amenities.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Hostels.Add(hostel);
                    await context.SaveChangesAsync();

                    var blockA = new HostelBlock
                    {
                        HostelId = hostel.Id,
                        Name = "Block A (Boys Wing - North)",
                        Description = "4-storey block housing undergraduate engineering students.",
                        TotalFloors = 4,
                        IsActive = true
                    };
                    var blockB = new HostelBlock
                    {
                        HostelId = hostel.Id,
                        Name = "Block B (Girls Wing - South)",
                        Description = "4-storey block with dedicated study zones and lounge.",
                        TotalFloors = 4,
                        IsActive = true
                    };
                    var blockC = new HostelBlock
                    {
                        HostelId = hostel.Id,
                        Name = "Block C (International & Postgraduate)",
                        Description = "Air-conditioned rooms for PG scholars and international students.",
                        TotalFloors = 3,
                        IsActive = true
                    };

                    context.HostelBlocks.AddRange(blockA, blockB, blockC);
                    await context.SaveChangesAsync();

                    // Seed sample rooms
                    var rooms = new List<HostelRoom>
                    {
                        new HostelRoom { BlockId = blockA.Id, RoomNumber = "A-101", Floor = 1, Capacity = 2, CurrentOccupancy = 2 },
                        new HostelRoom { BlockId = blockA.Id, RoomNumber = "A-102", Floor = 1, Capacity = 2, CurrentOccupancy = 1 },
                        new HostelRoom { BlockId = blockA.Id, RoomNumber = "A-204", Floor = 2, Capacity = 2, CurrentOccupancy = 2 },
                        new HostelRoom { BlockId = blockA.Id, RoomNumber = "A-305", Floor = 3, Capacity = 2, CurrentOccupancy = 1 },
                        new HostelRoom { BlockId = blockB.Id, RoomNumber = "B-108", Floor = 1, Capacity = 2, CurrentOccupancy = 2 },
                        new HostelRoom { BlockId = blockB.Id, RoomNumber = "B-212", Floor = 2, Capacity = 2, CurrentOccupancy = 1 },
                        new HostelRoom { BlockId = blockC.Id, RoomNumber = "C-101", Floor = 1, Capacity = 1, CurrentOccupancy = 1 }
                    };
                    context.HostelRooms.AddRange(rooms);
                    await context.SaveChangesAsync();
                }

                var defaultBlock = await context.HostelBlocks.FirstOrDefaultAsync();

                // 3. Seed Complaint Categories
                if (!await context.ComplaintCategories.AnyAsync())
                {
                    var categories = new List<ComplaintCategory>
                    {
                        new ComplaintCategory { Name = "Electrical Issues", Description = "Lights, fans, switches, plugs, and short circuits", Icon = "bi-lightning-charge", IsActive = true },
                        new ComplaintCategory { Name = "Plumbing Issues", Description = "Leaking faucets, pipeline bursts, washroom drainage", Icon = "bi-droplet-half", IsActive = true },
                        new ComplaintCategory { Name = "Cleaning and Hygiene", Description = "Corridor cleanliness, garbage collection, pest control", Icon = "bi-stars", IsActive = true },
                        new ComplaintCategory { Name = "Wi-Fi and Internet", Description = "Router connection, slow speed, LAN port issues", Icon = "bi-wifi", IsActive = true },
                        new ComplaintCategory { Name = "Furniture Damage", Description = "Beds, study desks, cupboards, door locks", Icon = "bi-lamp", IsActive = true },
                        new ComplaintCategory { Name = "Water Supply", Description = "Drinking water RO purifiers, water timing, hot geysers", Icon = "bi-cup-straw", IsActive = true },
                        new ComplaintCategory { Name = "Room Maintenance", Description = "Wall seepage, paint peeling, window glass repair", Icon = "bi-door-closed", IsActive = true },
                        new ComplaintCategory { Name = "Security Issues", Description = "ID checking, gate security, unauthorized entry concerns", Icon = "bi-shield-exclamation", IsActive = true },
                        new ComplaintCategory { Name = "Food and Mess", Description = "Mess food quality, cleanliness, dining hall facilities", Icon = "bi-cup-hot", IsActive = true },
                        new ComplaintCategory { Name = "Other", Description = "General hostel issues and student grievances", Icon = "bi-three-dots", IsActive = true }
                    };
                    context.ComplaintCategories.AddRange(categories);
                    await context.SaveChangesAsync();
                }

                // 4. Seed Users
                // A. Warden
                var wardenEmail = "warden@hostelcare.com";
                var warden = await userManager.FindByEmailAsync(wardenEmail);
                if (warden == null)
                {
                    warden = new ApplicationUser
                    {
                        UserName = wardenEmail,
                        Email = wardenEmail,
                        FullName = "Dr. Rajeshwar Rao (Chief Warden)",
                        StudentId = "EMP-WARDEN-01",
                        PhoneNumber = "9876543210",
                        UserRole = UserRoleType.Warden,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(warden, "Warden@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(warden, "Warden");
                    }
                }

                // B. Maintenance Staff (Electrician & Plumber)
                var elecEmail = "electrician@hostelcare.com";
                var electrician = await userManager.FindByEmailAsync(elecEmail);
                if (electrician == null)
                {
                    electrician = new ApplicationUser
                    {
                        UserName = elecEmail,
                        Email = elecEmail,
                        FullName = "Sunil Verma (Senior Electrician)",
                        StudentId = "EMP-MAINT-101",
                        PhoneNumber = "9876543222",
                        UserRole = UserRoleType.MaintenanceStaff,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(electrician, "Staff@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(electrician, "MaintenanceStaff");
                    }
                }

                var plumbEmail = "plumber@hostelcare.com";
                var plumber = await userManager.FindByEmailAsync(plumbEmail);
                if (plumber == null)
                {
                    plumber = new ApplicationUser
                    {
                        UserName = plumbEmail,
                        Email = plumbEmail,
                        FullName = "Manoj Kumar (Plumbing Specialist)",
                        StudentId = "EMP-MAINT-102",
                        PhoneNumber = "9876543233",
                        UserRole = UserRoleType.MaintenanceStaff,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(plumber, "Staff@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(plumber, "MaintenanceStaff");
                    }
                }

                // C. Student (Aarav Sharma)
                var studentEmail = "student@hostelcare.com";
                var student = await userManager.FindByEmailAsync(studentEmail);
                if (student == null)
                {
                    student = new ApplicationUser
                    {
                        UserName = studentEmail,
                        Email = studentEmail,
                        FullName = "Aarav Sharma",
                        StudentId = "2023BTE042",
                        PhoneNumber = "9876543244",
                        BlockId = defaultBlock?.Id,
                        RoomNumber = "A-204",
                        UserRole = UserRoleType.Student,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-3)
                    };
                    var result = await userManager.CreateAsync(student, "Student@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(student, "Student");
                    }
                }

                // D. Second Student (Priya Patel)
                var student2Email = "student2@hostelcare.com";
                var student2 = await userManager.FindByEmailAsync(student2Email);
                if (student2 == null)
                {
                    var blockB = await context.HostelBlocks.FirstOrDefaultAsync(b => b.Name.Contains("Block B"));
                    student2 = new ApplicationUser
                    {
                        UserName = student2Email,
                        Email = student2Email,
                        FullName = "Priya Patel",
                        StudentId = "2023BTE089",
                        PhoneNumber = "9876543255",
                        BlockId = blockB?.Id ?? defaultBlock?.Id,
                        RoomNumber = "B-108",
                        UserRole = UserRoleType.Student,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    };
                    var result = await userManager.CreateAsync(student2, "Student@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(student2, "Student");
                    }
                }

                // 5. Seed Sample Complaints with Full Status History & Comments
                if (!await context.Complaints.AnyAsync())
                {
                    var catElec = await context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name.Contains("Electrical"));
                    var catPlumb = await context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name.Contains("Plumbing"));
                    var catWifi = await context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name.Contains("Wi-Fi"));
                    var catFurn = await context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name.Contains("Furniture"));
                    var catWater = await context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name.Contains("Water"));

                    // Complaint 1: Urgent Electrical Issue - InProgress
                    var comp1 = new Complaint
                    {
                        ReferenceNumber = "HC-2026-0001",
                        Title = "Ceiling fan sparking and vibrating vigorously",
                        Description = "The ceiling fan in Room A-204 started emitting sparks when turned on at speed 4. High humming sound and intense vibration observed. Need immediate electrical inspection for fire safety.",
                        CategoryId = catElec?.Id ?? 1,
                        Priority = ComplaintPriority.Urgent,
                        Status = ComplaintStatus.InProgress,
                        StudentId = student.Id,
                        BlockId = student.BlockId,
                        RoomNumber = student.RoomNumber ?? "A-204",
                        AssignedStaffId = electrician.Id,
                        CreatedAt = DateTime.UtcNow.AddHours(-18),
                        UpdatedAt = DateTime.UtcNow.AddHours(-2),
                        AdminResponse = "Technician dispatched on high priority due to fire hazard precaution."
                    };
                    context.Complaints.Add(comp1);
                    await context.SaveChangesAsync();

                    context.ComplaintStatusHistories.AddRange(
                        new ComplaintStatusHistory
                        {
                            ComplaintId = comp1.Id,
                            PreviousStatus = ComplaintStatus.Submitted,
                            NewStatus = ComplaintStatus.Submitted,
                            ChangedById = student.Id,
                            ChangedAt = DateTime.UtcNow.AddHours(-18),
                            Notes = "Complaint submitted by student with Urgent priority."
                        },
                        new ComplaintStatusHistory
                        {
                            ComplaintId = comp1.Id,
                            PreviousStatus = ComplaintStatus.Submitted,
                            NewStatus = ComplaintStatus.Assigned,
                            ChangedById = warden.Id,
                            ChangedAt = DateTime.UtcNow.AddHours(-12),
                            Notes = "Assigned to Sunil Verma (Senior Electrician)."
                        },
                        new ComplaintStatusHistory
                        {
                            ComplaintId = comp1.Id,
                            PreviousStatus = ComplaintStatus.Assigned,
                            NewStatus = ComplaintStatus.InProgress,
                            ChangedById = electrician.Id,
                            ChangedAt = DateTime.UtcNow.AddHours(-2),
                            Notes = "Regulator and capacitor inspected. Replacement capacitor ordered from central store."
                        }
                    );

                    context.ComplaintComments.AddRange(
                        new ComplaintComment
                        {
                            ComplaintId = comp1.Id,
                            UserId = warden.Id,
                            CommentText = "Please keep the fan switch turned OFF until technician replaces the capacitor.",
                            CreatedAt = DateTime.UtcNow.AddHours(-11),
                            IsInternal = false
                        },
                        new ComplaintComment
                        {
                            ComplaintId = comp1.Id,
                            UserId = electrician.Id,
                            CommentText = "New capacitor will be fitted tomorrow morning 10 AM.",
                            CreatedAt = DateTime.UtcNow.AddHours(-1),
                            IsInternal = false
                        }
                    );

                    // Complaint 2: Plumbing Issue - InProgress
                    var comp2 = new Complaint
                    {
                        ReferenceNumber = "HC-2026-0002",
                        Title = "Washroom tap dripping continuously causing floor flooding",
                        Description = "The washbasin mixer tap in bathroom A-204 doesn't shut completely. Water is accumulating and causing slippery floor conditions.",
                        CategoryId = catPlumb?.Id ?? 2,
                        Priority = ComplaintPriority.High,
                        Status = ComplaintStatus.Assigned,
                        StudentId = student.Id,
                        BlockId = student.BlockId,
                        RoomNumber = student.RoomNumber ?? "A-204",
                        AssignedStaffId = plumber.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddHours(-4)
                    };
                    context.Complaints.Add(comp2);
                    await context.SaveChangesAsync();

                    context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                    {
                        ComplaintId = comp2.Id,
                        PreviousStatus = ComplaintStatus.Submitted,
                        NewStatus = ComplaintStatus.Assigned,
                        ChangedById = warden.Id,
                        ChangedAt = DateTime.UtcNow.AddHours(-4),
                        Notes = "Assigned to Manoj Kumar for rubber washer replacement."
                    });

                    // Complaint 3: Wi-Fi Issue - Under Review
                    var comp3 = new Complaint
                    {
                        ReferenceNumber = "HC-2026-0003",
                        Title = "Wi-Fi access point in 2nd floor corridor dropping signal",
                        Description = "The 5GHz SSID disconnects every 10 minutes when multiple students join evening online classes. 2.4GHz is overloaded.",
                        CategoryId = catWifi?.Id ?? 4,
                        Priority = ComplaintPriority.Medium,
                        Status = ComplaintStatus.UnderReview,
                        StudentId = student.Id,
                        BlockId = student.BlockId,
                        RoomNumber = student.RoomNumber ?? "A-204",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        AdminResponse = "Network administrator contacted for AP firmware reboot."
                    };
                    context.Complaints.Add(comp3);
                    await context.SaveChangesAsync();

                    // Complaint 4: Furniture Damage - Resolved
                    var comp4 = new Complaint
                    {
                        ReferenceNumber = "HC-2026-0004",
                        Title = "Study chair caster wheel detached",
                        Description = "The right caster wheel of the revolving study chair came off its socket.",
                        CategoryId = catFurn?.Id ?? 5,
                        Priority = ComplaintPriority.Low,
                        Status = ComplaintStatus.Resolved,
                        StudentId = student.Id,
                        BlockId = student.BlockId,
                        RoomNumber = student.RoomNumber ?? "A-204",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3),
                        ResolvedAt = DateTime.UtcNow.AddDays(-3),
                        ResolutionNotes = "New heavy-duty caster wheel fitted. Tested and verified with student."
                    };
                    context.Complaints.Add(comp4);
                    await context.SaveChangesAsync();

                    context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                    {
                        ComplaintId = comp4.Id,
                        PreviousStatus = ComplaintStatus.InProgress,
                        NewStatus = ComplaintStatus.Resolved,
                        ChangedById = warden.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-3),
                        Notes = "Work verified and marked resolved."
                    });

                    // Complaint 5: Water Supply - Submitted
                    var comp5 = new Complaint
                    {
                        ReferenceNumber = "HC-2026-0005",
                        Title = "Low water pressure in 1st floor washrooms",
                        Description = "During morning hours from 7:00 AM to 8:30 AM, water pressure in Block B 1st floor is severely diminished.",
                        CategoryId = catWater?.Id ?? 6,
                        Priority = ComplaintPriority.High,
                        Status = ComplaintStatus.Submitted,
                        StudentId = student2.Id,
                        BlockId = student2.BlockId,
                        RoomNumber = student2.RoomNumber ?? "B-108",
                        CreatedAt = DateTime.UtcNow.AddHours(-6)
                    };
                    context.Complaints.Add(comp5);
                    await context.SaveChangesAsync();

                    // 6. Seed Sample Notifications
                    var notifications = new List<Notification>
                    {
                        new Notification
                        {
                            UserId = student.Id,
                            Title = "Complaint Under Investigation",
                            Message = "Your electrical complaint HC-2026-0001 has been assigned to Sunil Verma.",
                            ComplaintId = comp1.Id,
                            Type = NotificationType.ComplaintAssigned,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddHours(-12)
                        },
                        new Notification
                        {
                            UserId = student.Id,
                            Title = "Technician Added Update",
                            Message = "Sunil Verma updated status of HC-2026-0001 to InProgress.",
                            ComplaintId = comp1.Id,
                            Type = NotificationType.StatusUpdated,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddHours(-2)
                        },
                        new Notification
                        {
                            UserId = student.Id,
                            Title = "Complaint Resolved",
                            Message = "Complaint HC-2026-0004 (Study chair caster wheel) was resolved.",
                            ComplaintId = comp4.Id,
                            Type = NotificationType.ComplaintResolved,
                            IsRead = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        },
                        new Notification
                        {
                            UserId = electrician.Id,
                            Title = "Urgent Electrical Task",
                            Message = "You were assigned urgent complaint HC-2026-0001 at Room A-204.",
                            ComplaintId = comp1.Id,
                            Type = NotificationType.ComplaintAssigned,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddHours(-12)
                        }
                    };
                    context.Notifications.AddRange(notifications);

                    // 7. Seed Sample Payment Demonstration
                    var payment = new HostelPayment
                    {
                        StudentId = student.Id,
                        Amount = 2500.00m,
                        FeeType = "Hostel Maintenance & Amenities Fee",
                        Description = "Semester 1 Maintenance contribution",
                        TransactionId = "TXN-HC-89214710",
                        PaymentDate = DateTime.UtcNow.AddDays(-15),
                        Status = PaymentStatus.Completed,
                        PaymentMethod = "Demo UPI"
                    };
                    context.HostelPayments.Add(payment);

                    await context.SaveChangesAsync();
                    logger.LogInformation("Database seeded successfully with initial users, hostels, categories, and sample complaints.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
