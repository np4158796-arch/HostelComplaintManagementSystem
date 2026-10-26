using System;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// 1. DATABASE & ENTITY FRAMEWORK CORE CONFIGURATION (IT5039 Unit III)
// =====================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=HostelCareDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// =====================================================================
// 2. ASP.NET CORE IDENTITY & AUTHENTICATION (IT5039 Unit II & VI)
// =====================================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password strength rules
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Account lockout rules
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Secure Authentication Cookie (IT5039 Unit II - Cookies)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.Name = ".HostelCare.Auth";
});

// Optional Google OAuth (IT5039 Unit VI)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

// =====================================================================
// 3. STATE MANAGEMENT: SESSIONS & CACHING (IT5039 Unit II & III)
// =====================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".HostelCare.Session";
});

// In-Memory Data Caching for reference tables (Categories, Blocks)
builder.Services.AddMemoryCache();

// HTTP Response Caching for public pages
builder.Services.AddResponseCaching();

// =====================================================================
// 4. DEPENDENCY INJECTION: CUSTOM BUSINESS SERVICES
// =====================================================================
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IReportService, ReportService>();

// =====================================================================
// 5. MVC CONTROLLERS, RAZOR VIEWS & SWAGGER / OPENAPI (IT5039 Unit IV & V)
// =====================================================================
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HostelCare REST API",
        Version = "v1",
        Description = "RESTful Web API for Hostel Complaint Management System - Subject: IT5039 Web Technology with .NET"
    });

    // Ensure Swagger only documents API endpoints and Minimal APIs, ignoring Razor View actions
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath != null && apiDesc.RelativePath.StartsWith("api", StringComparison.OrdinalIgnoreCase);
    });
});

var app = builder.Build();

// =====================================================================
// 6. HTTP REQUEST PIPELINE & MIDDLEWARE (IT5039 Unit III)
// Order of middleware execution is critical in ASP.NET Core:
// 1. Exception Handling / Diagnostics
// 2. HTTPS Redirection & HSTS
// 3. Static Files
// 4. Routing
// 5. Response Caching
// 6. Session
// 7. Authentication
// 8. Authorization
// 9. Endpoint Mapping (Controllers & Minimal APIs)
// =====================================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HostelCare API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// =====================================================================
// 7. MINIMAL API ENDPOINTS (IT5039 Unit IV - Web Services)
// Provides lightweight, high-performance telemetry endpoints
// =====================================================================
app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "Healthy",
    system = "HostelCare Complaint Management System",
    timestamp = DateTime.UtcNow,
    framework = ".NET 8 LTS"
})).WithTags("Health");

app.MapGet("/api/v1/hostel/stats", async (ApplicationDbContext db) =>
{
    var total = await db.Complaints.CountAsync();
    var resolved = await db.Complaints.CountAsync(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);
    var pending = total - resolved;
    var totalRooms = await db.HostelRooms.CountAsync();

    return Results.Ok(new
    {
        totalComplaints = total,
        resolvedComplaints = resolved,
        pendingComplaints = pending,
        totalRoomsRegistered = totalRooms,
        resolutionRate = total > 0 ? Math.Round((double)resolved / total * 100, 1) : 0
    });
}).WithTags("Hostel Public Stats");

// =====================================================================
// 8. CONTROLLER ROUTE MAPPING
// =====================================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =====================================================================
// 9. AUTOMATIC DATABASE CREATION & SEEDING ON STARTUP
// =====================================================================
await DbInitializer.InitializeAsync(app.Services);

app.Run();
