# HostelCare — Hostel Complaint Management System
**Tagline:** *"Your Complaint. Our Responsibility."*  
**Academic Course:** IT5039 – Web Technology with .NET  
**Program:** B.Tech Computer Engineering (Semester Project & Viva Examination)  
**Target Framework:** ASP.NET Core 8.0 (LTS)  
**Database:** Microsoft SQL Server LocalDB with Entity Framework Core  

---

## 1. Project Introduction & Overview
**HostelCare** is a production-grade, responsive, full-stack web application developed to modernize grievance redressal, routine room repairs, and emergency facilities maintenance across university hostel residential blocks. It replaces slow, error-prone paper registers and untracked WhatsApp requests with a transparent, role-based, digitally audited grievance lifecycle.

### Problem Statement
In traditional college hostels, students report maintenance concerns (such as sparking electrical fans, leaking faucets, slow Wi-Fi access points, and damaged study furniture) through manual complaint registers kept at security gates or warden offices. This process suffers from:
* Lack of real-time tracking: Students cannot determine whether an electrician has been dispatched.
* Loss of accountability: Technicians have no verified record of room assignments or repair history.
* Poor escalation: Urgent life-safety issues (such as electrical short circuits) sit in registers alongside routine requests.
* Absence of administrative analytics: Chief Wardens lack data regarding recurring vendor defects, SLA breach rates, or technician workloads.

### Project Objectives
1. **End-to-End Complaint Lifecycle:** Enable students to submit grievances with category classification, priority tagging, room pre-population, and photo evidence.
2. **Role-Based Workflows:** Distinct, customized dashboards and permissions for **Students**, **Wardens (Administrators)**, and **Maintenance Staff (Electricians, Plumbers, Technicians)**.
3. **Real-Time Auditing:** Automatically capture timestamped status transitions (`Submitted` &rarr; `UnderReview` &rarr; `Assigned` &rarr; `InProgress` &rarr; `Resolved` &rarr; `Closed` / `Reopened`).
4. **Academic Syllabus Alignment:** Fully demonstrate every theoretical and practical concept from the **IT5039 Web Technology with .NET** curriculum.

---

## 2. Technology Stack & Architecture
* **Backend:** C# 12 and ASP.NET Core 8.0 MVC (Model-View-Controller)
* **Frontend:** Razor Views (`.cshtml`), HTML5, CSS3, JavaScript (ES6+), Bootstrap 5.3, Bootstrap Icons 1.11, and Chart.js 4.4
* **Database Engine:** Microsoft SQL Server LocalDB (`MSSQLLocalDB`)
* **ORM:** Entity Framework Core 8.0 (Code-First with Migrations)
* **Identity & Security:** ASP.NET Core Identity with PBKDF2 password hashing, secure auth cookies, anti-forgery tokens (`ValidateAntiForgeryToken`), and role-based policies
* **Web Services:** ASP.NET Core RESTful Controller API (`/api/complaints`) with HTTP GET, POST, PUT, PATCH, and ASP.NET Core Minimal APIs (`/api/v1/health`, `/api/v1/hostel/stats`)
* **API Documentation:** OpenAPI / Swagger UI via Swashbuckle (`/swagger`)
* **State Management:** Identity Cookies, ASP.NET Core Session (`ISession`), `ViewBag`, `ViewData`, and `TempData` flash toasts
* **Caching:** In-Memory Caching (`IMemoryCache`) with programmatic invalidation and HTTP Output/Response Caching (`[ResponseCache]`)

---

## 3. System Architecture & MVC Workflow
The application follows a clean, decoupled MVC multi-tier enterprise structure:

```
HostelComplaintManagementSystem/
│
├── Controllers/
│   ├── HomeController.cs                # Landing page, public counters, error handling (403, 404, 500)
│   ├── AccountController.cs             # Registration, Login, RememberMe, Logout, Google OAuth handler
│   ├── StudentController.cs             # Student dashboard, complaint lodging, session filter, reopen
│   ├── ComplaintsController.cs          # Details tracking, public tracker, comment threads
│   ├── WardenController.cs              # Master dashboard, assignment, status update, user & room admin
│   ├── StaffController.cs               # Technician workspace, task acceptance, work progress updates
│   ├── NotificationsController.cs       # Notification center, mark read, badge count
│   ├── ReportsController.cs             # Analytics, SLA metrics, staff scorecards, CSV export
│   ├── ProfileController.cs             # Personal details, room updates, password modification
│   ├── PaymentDemoController.cs         # Simulated fee checkout, transaction auditing
│   └── Api/
│       └── ComplaintsApiController.cs   # RESTful API with HTTP GET, POST, PUT, PATCH, OpenAPI DTOs
│
├── Models/
│   ├── Enums.cs                         # ComplaintPriority, ComplaintStatus, UserRoleType, PaymentStatus
│   ├── ApplicationUser.cs               # IdentityUser extension (StudentId, RoomNumber, BlockId, UserRole)
│   ├── Hostel.cs                        # Hostel, HostelBlock, and HostelRoom entities
│   ├── ComplaintCategory.cs             # 10 issue categories with icons and active status
│   ├── Complaint.cs                     # Central complaint entity with HC-2026-XXXX reference
│   ├── ComplaintActivity.cs             # ComplaintAssignment, StatusHistory, ComplaintComment
│   └── NotificationAndPayment.cs        # Notification, EmailNotificationLog, HostelPayment
│
├── Data/
│   ├── ApplicationDbContext.cs          # EF Core context, relationships, precision, indexes
│   ├── DbInitializer.cs                 # Auto-migration and comprehensive sample database seeding
│   └── Migrations/                      # EF Core Code-First migration history files
│
├── Services/
│   ├── IComplaintService.cs             # Complaint lifecycle, reference generation, status transitions
│   ├── INotificationService.cs          # In-app notifications & unread counts
│   ├── IEmailService.cs                 # Configurable SMTP delivery & development logger fallback
│   ├── IFileStorageService.cs           # Evidence upload, GUID filenames, path traversal security
│   ├── IReportService.cs                # Dashboard KPIs, Chart.js metrics, and CSV generation
│   └── ICacheService.cs                 # In-Memory Cache for categories/blocks with invalidation
│
├── ViewModels/                          # Strongly typed DTOs for forms and analytical displays
├── DTOs/                                # REST API request and response data contracts
├── Views/                               # Responsive Bootstrap 5 Razor Views
├── wwwroot/                             # CSS, custom JavaScript, uploads directory
└── Program.cs                           # Request pipeline, middleware, and dependency injection
```

---

## 4. User Roles & Capabilities

| Feature / Permission | Student Resident | Warden / Administrator | Maintenance Technician |
| :--- | :---: | :---: | :---: |
| Self-Registration & Login | Yes (Student only) | Admin provisioned | Admin provisioned |
| Personal Dashboard & Metrics | Yes (Own records) | Yes (Hostel-wide) | Yes (Assigned tasks) |
| Submit Complaint & Photo Evidence | Yes | Yes (On student behalf)| No |
| Track Status Journey Timeline | Yes (Own complaints) | Yes (All complaints) | Yes (Assigned tasks) |
| Reopen Resolved Complaints | Yes (With reason) | Yes | No |
| Assign / Reassign Maintenance Staff | No | Yes | No |
| Update Status to InProgress / Resolved | No | Yes | Yes (Only assigned task) |
| Post Public Comments & Queries | Yes | Yes | Yes |
| Post Internal Administrative Notes | No | Yes | Yes |
| Manage Hostel Blocks, Rooms & Categories | No | Yes | No |
| Export Analytical Reports to CSV | No | Yes | No |
| Online Fee Demonstration Checkout | Yes | View records | View records |

---

## 5. Seed Demo Accounts for Viva Testing

The application automatically seeds ready-to-test accounts on first launch. In addition, the **Login page includes 1-Click Fill buttons** for effortless viva presentations:

| Role | Email Address | Password | Profile / Designation |
| :--- | :--- | :--- | :--- |
| **Chief Warden** | `warden@hostelcare.com` | `Warden@123` | Dr. Rajeshwar Rao (Admin Desk) |
| **Senior Electrician** | `electrician@hostelcare.com` | `Staff@123` | Sunil Verma (Maintenance Wing) |
| **Plumbing Specialist**| `plumber@hostelcare.com` | `Staff@123` | Manoj Kumar (Maintenance Wing) |
| **Student Resident 1** | `student@hostelcare.com` | `Student@123` | Aarav Sharma (Room A-204, Block A) |
| **Student Resident 2** | `student2@hostelcare.com` | `Student@123` | Priya Patel (Room B-108, Block B) |

---

## 6. Pre-Seeded Sample Complaints (Diverse Workflow Stages)

* **HC-2026-0001:** *"Ceiling fan sparking and vibrating vigorously"* (Electrical Issues &bull; **Urgent** &bull; **In Progress** &bull; Assigned to Sunil Verma). Demonstrates status history with capacitor replacement notes and public communication thread.
* **HC-2026-0002:** *"Washroom tap dripping continuously causing floor flooding"* (Plumbing Issues &bull; **High** &bull; **Assigned** &bull; Assigned to Manoj Kumar). Demonstrates warden triage and technician dispatch.
* **HC-2026-0003:** *"Wi-Fi access point in 2nd floor corridor dropping signal"* (Wi-Fi and Internet &bull; **Medium** &bull; **Under Review**). Demonstrates pending administrative triage.
* **HC-2026-0004:** *"Study chair caster wheel detached"* (Furniture Damage &bull; **Low** &bull; **Resolved**). Demonstrates technician resolution notes and student's **Reopen** option.
* **HC-2026-0005:** *"Low water pressure in 1st floor washrooms"* (Water Supply &bull; **High** &bull; **Submitted**). Demonstrates brand new student lodging.

---

## 7. IT5039 Syllabus Mapping (Mandatory Academic Evidence)

This project was built to provide direct code references for viva examination topics under **IT5039 – Web Technology with .NET**:

### Unit I — Introduction & MVC Fundamentals
* **Project Structure & Startup:** Configured in `Program.cs` and `HostelComplaintManagementSystem.csproj`.
* **Data Models:** Located in `Models/` with 12 interconnected domain entities.
* **Controller Actions & Strongly-Typed Views:** Found across all controllers and Razor views in `Views/`.
* **Form Building & Validation:** Razor TagHelpers (`asp-for`, `asp-validation-for`) in `SubmitComplaint.cshtml` and `Register.cshtml`.
* **CSS & Design System:** Custom styling in `wwwroot/css/site.css` featuring deep navy, violet accents, and responsive layout.

### Unit II — Web Application Development Basics
* **Cookies:** ASP.NET Core Identity authentication cookies configured in `Program.cs` with `HttpOnly = true`, `SameSite = Lax`, and `CookieSecurePolicy.SameAsRequest`.
* **Sessions (`ISession`):** Demonstrated in `StudentController.cs` (`MyComplaints` action), where the student's selected status filter is remembered across requests via `HttpContext.Session.SetString()`.
* **HTTPS & Pipeline:** `app.UseHttpsRedirection()` and `app.UseHsts()` in `Program.cs`.
* **Error Handling:** Friendly error routes configured for 404 Not Found, 403 Forbidden, and 500 Server Error in `HomeController.cs` without exposing stack traces.

### Unit III — Working with Data
* **Entity Framework Core:** `ApplicationDbContext.cs` configured with SQL Server LocalDB, custom indexes, foreign key delete restrictions, and decimal precision.
* **EF Core Migrations:** Code-First migrations created via `dotnet ef migrations add InitialCreate` and applied via `context.Database.MigrateAsync()`.
* **Data Caching (`IMemoryCache`):** Implemented in `Services/CacheService.cs` for active categories and hostel blocks. Demonstrates cache hit/miss logging and **programmatic cache invalidation** in `WardenController.cs` (`AddCategory` and `ToggleCategoryStatus`).
* **HTTP Output/Response Caching:** Public landing page cached via `[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]` in `HomeController.cs`.

### Unit IV — Web Services
* **RESTful Controller API:** Implemented in `Controllers/Api/ComplaintsApiController.cs` at `/api/complaints`.
* **HTTP Verbs:** Full implementation of `GET`, `POST`, `PUT`, and `PATCH`.
* **HTTP PATCH Status Update:** `PATCH /api/complaints/{id}/status` validates permissible workflow transitions and updates only the status, resolution notes, and audit history.
* **Minimal API:** `app.MapGet("/api/v1/health", ...)` and `app.MapGet("/api/v1/hostel/stats", ...)` in `Program.cs` demonstrate lightweight service endpoints.
* **API Documentation & Exploration:** OpenAPI 3.0 specification generated and accessible interactively via Swagger UI at `/swagger`.

### Unit V — Using Controllers with Views
* **Razor Syntax & Helpers:** Demonstrated across all `.cshtml` files using `@model`, `@if`, `@foreach`, and TagHelpers.
* **ViewBag & ViewData:** Used in `HomeController.cs`, `StudentController.cs`, and `ReportsController.cs` for lightweight non-sensitive view data and breadcrumbs.
* **TempData:** Flash toast banners implemented in `_AlertMessages.cshtml` and populated via `TempData["SuccessMessage"]` and `TempData["ErrorMessage"]`.
* **Reusable Partial Views:** `_StatusBadgePartial.cshtml`, `_PriorityBadgePartial.cshtml`, `_AlertMessages.cshtml`, and `_ValidationScriptsPartial.cshtml`.

### Unit VI — Working with Models, Forms & Other Web Applications
* **Model Binding & Data Annotations:** Robust validation attributes (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[Phone]`, `[Compare]`, `[Range]`) in `ViewModels/`.
* **File Upload Handling:** `FileStorageService.cs` inspects file sizes (5MB max), validates permitted extensions (`.jpg`, `.jpeg`, `.png`, `.webp`), generates non-colliding GUID filenames, and guards against path traversal.
* **Email Notification Service:** `EmailService.cs` handles configurable SMTP delivery with automatic development logger fallback and audit logging to the `EmailNotificationLog` table.
* **Online Payment Demonstration:** Implemented in `PaymentDemoController.cs` and `PaymentDemo/Index.cshtml` with simulated success/failure flows and receipt generation.
* **Social Login (Google OAuth):** Handled gracefully in `AccountController.cs` (`ExternalLogin` & `ExternalLoginCallback`) with clear configuration guidance.

---

## 8. REST API Endpoints Reference

| Method | Endpoint | Description | Auth / Scope |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/complaints` | Retrieves complaints list (filtered by caller role) | Role-filtered |
| `GET` | `/api/complaints/{id}` | Retrieves full details of a complaint by ID | Ownership checked |
| `POST` | `/api/complaints` | Lodges a new complaint with DTO validation | Student / Auth |
| `PUT` | `/api/complaints/{id}` | Updates complaint title, description, and priority | Owner / Admin |
| `PATCH`| `/api/complaints/{id}/status` | Executes status transition with validation & audit | Warden / Staff |
| `GET` | `/api/complaints/{id}/history`| Returns complete status transition history | Authorized users |
| `GET` | `/api/complaints/categories` | Returns active categories from cache | Public / Cache |
| `GET` | `/api/v1/health` | Minimal API: Health probe and runtime metadata | Public |
| `GET` | `/api/v1/hostel/stats` | Minimal API: Public aggregate hostel statistics | Public |

---

## 9. Setup & Running Instructions

### Prerequisites
* Windows 10 or 11
* .NET 8.0 SDK (or later)
* Microsoft Visual Studio 2022 (Community or Professional with ASP.NET and web development workload) or Visual Studio Code
* SQL Server LocalDB (`MSSQLLocalDB` — included by default with Visual Studio)

### Step 1: Clone or Open Project
Open the folder in Visual Studio:
`File` &rarr; `Open` &rarr; `Project/Solution` &rarr; Select `HostelComplaintManagementSystem.csproj`.

### Step 2: Database Migration & Creation
The application executes automatic migrations and database seeding on first startup. However, you can also run the standard EF Core CLI command:

```powershell
# Restore dependencies and build
dotnet build

# Apply EF Core migrations to LocalDB
dotnet ef database update
```

### Step 3: Run the Website
Press **F5** (or `Ctrl + F5`) in Visual Studio, or run in PowerShell:

```powershell
dotnet run
```

The application will start listening on `http://localhost:5000` (or the Visual Studio IIS Express / Kestrel port).

### Step 4: Explore the Features
1. **Public Site:** Visit `http://localhost:5000` to see the live metrics counter and 3-step guide.
2. **Interactive Swagger Docs:** Visit `http://localhost:5000/swagger` to inspect and test all REST endpoints.
3. **Log in as Student:** Click **Login** &rarr; click the blue **Student** demo button &rarr; explore **My Dashboard**, **Submit Complaint** (with photo upload), and **My Complaints**.
4. **Log in as Warden:** Click **Switch Account** &rarr; click the red **Warden** demo button &rarr; explore **Warden Desk**, **Manage Complaints**, **Assign Staff**, **Manage Users**, **Manage Categories**, and **Reports** (with CSV export).
5. **Log in as Staff:** Click the green **Staff** demo button &rarr; explore **Technician Portal**, **Accept Task**, and **Update Progress**.
6. **Track by ID:** Visit **Track Complaint** and search `HC-2026-0001` to view the public status journey.

---

## 10. External Integrations & Configuration (Optional)

### A. SMTP Email Notifications
Email notifications automatically simulate in the console and persist to the database `EmailNotificationLog` table. To connect a live SMTP provider (e.g., Gmail, SendGrid), update `appsettings.json`:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "noreply@hostelcare.com",
  "EnableSsl": "true"
}
```

### B. Google OAuth Social Login
Google OAuth is wired into `AccountController.cs`. When not configured, a clear indicator explains that email & password authentication is active. To enable Google sign-in:
1. Create OAuth credentials in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials).
2. Set the Authorized Redirect URI to: `https://localhost:port/signin-google`.
3. Add the keys to `appsettings.json` or User Secrets:
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
       "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
     }
   }
   ```

---

## 11. Viva Examination Questions & Answers Cheat Sheet

* **Q: Why use EF Core Migrations instead of manual SQL scripts?**  
  *A:* Migrations keep database schema synchronized with C# domain models in code version control. It creates an `__EFMigrationsHistory` table to ensure atomic, reproducible upgrades across team environments.
* **Q: How are multiple cascade deletion paths avoided in SQL Server?**  
  *A:* In `ApplicationDbContext.cs`, relations on `Complaint.StudentId` and `Complaint.AssignedStaffId` are configured with `DeleteBehavior.Restrict` so deleting a user does not trigger conflicting recursive cascades.
* **Q: What is the difference between Controller API and Minimal API here?**  
  *A:* `ComplaintsApiController` utilizes controller-based structure with rich model binding and DTO filters for CRUD operations. Minimal APIs (`/api/v1/health` and `/api/v1/hostel/stats`) are mapped directly in `Program.cs` for maximum throughput with minimal routing overhead.
* **Q: How does the application prevent duplicate complaint reference collisions?**  
  *A:* `ComplaintService.GenerateReferenceNumberAsync()` queries the highest current sequence for the active year in the database, locks the generator, and formats `HC-yyyy-####` with a unique SQL index constraint on `Complaint.ReferenceNumber`.
* **Q: How is memory caching invalidated?**  
  *A:* `CacheService.cs` stores categories under a memory key with a 15-minute sliding expiration. When a warden creates or toggles a category in `WardenController.cs`, `_cacheService.InvalidateCategoriesCache()` removes the key, forcing the next read to reload from SQL.

---

## 12. Conclusion & Academic Declaration
This project implements a fully working, secure, and visually polished **Hostel Complaint Management System** fulfilling all requirements of the **IT5039 – Web Technology with .NET** curriculum. All features, controllers, views, database migrations, and REST APIs are verified functional and ready for evaluation.
