using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.DTOs;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers.Api
{
    [ApiController]
    [Route("api/complaints")]
    [Produces("application/json")]
    public class ComplaintsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IComplaintService _complaintService;
        private readonly ICacheService _cacheService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ComplaintsApiController> _logger;

        public ComplaintsApiController(
            ApplicationDbContext context,
            IComplaintService complaintService,
            ICacheService cacheService,
            UserManager<ApplicationUser> userManager,
            ILogger<ComplaintsApiController> logger)
        {
            _context = context;
            _complaintService = complaintService;
            _cacheService = cacheService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves list of complaints filtered by caller's role (IT5039 Unit IV).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintResponseDto>>> GetComplaints([FromQuery] ComplaintStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);

            var query = _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .AsQueryable();

            if (user != null && user.UserRole == UserRoleType.Student)
            {
                query = query.Where(c => c.StudentId == user.Id);
            }
            else if (user != null && user.UserRole == UserRoleType.MaintenanceStaff)
            {
                query = query.Where(c => c.AssignedStaffId == user.Id);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .Select(c => MapToDto(c))
                .ToListAsync();

            return Ok(complaints);
        }

        /// <summary>
        /// Retrieves detailed information for a single complaint.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintResponseDto>> GetComplaint(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var complaint = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return NotFound(new { error = $"Complaint with ID {id} not found." });
            }

            // Authorization check
            if (user != null && user.UserRole == UserRoleType.Student && complaint.StudentId != user.Id)
            {
                return Forbid();
            }

            return Ok(MapToDto(complaint));
        }

        /// <summary>
        /// Registers a new complaint via REST API.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ComplaintResponseDto>> CreateComplaint([FromBody] ComplaintCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            var studentId = user?.Id;

            // If unauthenticated in development, bind to first seed student
            if (string.IsNullOrEmpty(studentId))
            {
                var defaultStudent = await _context.Users.FirstOrDefaultAsync(u => u.UserRole == UserRoleType.Student);
                studentId = defaultStudent?.Id;
                if (string.IsNullOrEmpty(studentId))
                {
                    return Unauthorized(new { error = "You must be authenticated as a student to submit a complaint." });
                }
            }

            var refNumber = await _complaintService.GenerateReferenceNumberAsync();

            var complaint = new Complaint
            {
                ReferenceNumber = refNumber,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                CategoryId = dto.CategoryId,
                Priority = dto.Priority,
                Status = ComplaintStatus.Submitted,
                StudentId = studentId,
                BlockId = dto.BlockId,
                RoomNumber = dto.RoomNumber.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                PreviousStatus = ComplaintStatus.Submitted,
                NewStatus = ComplaintStatus.Submitted,
                ChangedById = studentId,
                ChangedAt = DateTime.UtcNow,
                Notes = "Submitted via REST API."
            };
            _context.ComplaintStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            // Load navigation properties for response DTO
            await _context.Entry(complaint).Reference(c => c.Category).LoadAsync();
            await _context.Entry(complaint).Reference(c => c.HostelBlock).LoadAsync();
            await _context.Entry(complaint).Reference(c => c.Student).LoadAsync();

            return CreatedAtAction(nameof(GetComplaint), new { id = complaint.Id }, MapToDto(complaint));
        }

        /// <summary>
        /// Updates complaint details (PUT).
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComplaint(int id, [FromBody] ComplaintUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null)
            {
                return NotFound(new { error = $"Complaint with ID {id} not found." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null && user.UserRole == UserRoleType.Student && complaint.StudentId != user.Id)
            {
                return Forbid();
            }

            complaint.Title = dto.Title.Trim();
            complaint.Description = dto.Description.Trim();
            complaint.CategoryId = dto.CategoryId;
            complaint.Priority = dto.Priority;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Performs status transition using HTTP PATCH (IT5039 Unit IV).
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateComplaintStatus(int id, [FromBody] ComplaintStatusPatchDto patchDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserRole == UserRoleType.Warden);
                userId = admin?.Id ?? "SYSTEM";
            }

            var result = await _complaintService.UpdateStatusAsync(
                id,
                patchDto.NewStatus,
                userId,
                patchDto.Notes,
                patchDto.AdminResponse);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var updated = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .FirstOrDefaultAsync(c => c.Id == id);

            return Ok(MapToDto(updated!));
        }

        /// <summary>
        /// Returns full status transition timeline.
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<ComplaintStatusHistoryDto>>> GetHistory(int id)
        {
            var histories = await _context.ComplaintStatusHistories
                .Include(h => h.ChangedBy)
                .Where(h => h.ComplaintId == id)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new ComplaintStatusHistoryDto
                {
                    Id = h.Id,
                    PreviousStatus = h.PreviousStatus.ToString(),
                    NewStatus = h.NewStatus.ToString(),
                    ChangedByName = h.ChangedBy != null ? h.ChangedBy.FullName : "System",
                    ChangedAt = h.ChangedAt,
                    Notes = h.Notes
                })
                .ToListAsync();

            return Ok(histories);
        }

        /// <summary>
        /// Returns active categories from cache.
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories()
        {
            var categories = await _cacheService.GetCachedCategoriesAsync();
            var dtos = categories.Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                IsActive = c.IsActive
            });

            return Ok(dtos);
        }

        private static ComplaintResponseDto MapToDto(Complaint c)
        {
            return new ComplaintResponseDto
            {
                Id = c.Id,
                ReferenceNumber = c.ReferenceNumber,
                Title = c.Title,
                Description = c.Description,
                CategoryId = c.CategoryId,
                CategoryName = c.Category?.Name ?? "Uncategorized",
                Priority = c.Priority.ToString(),
                Status = c.Status.ToString(),
                StudentName = c.Student?.FullName ?? "N/A",
                BlockName = c.HostelBlock?.Name,
                RoomNumber = c.RoomNumber,
                AssignedStaffName = c.AssignedStaff?.FullName,
                ImageAttachmentUrl = c.ImageAttachmentUrl,
                AdminResponse = c.AdminResponse,
                CreatedAt = c.CreatedAt,
                ResolvedAt = c.ResolvedAt
            };
        }
    }
}
