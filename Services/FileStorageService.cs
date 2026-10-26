using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Services
{
    public interface IFileStorageService
    {
        Task<(bool Success, string? FilePath, string? ErrorMessage)> SaveImageAsync(IFormFile? file, string subFolder = "complaints");
        void DeleteImage(string relativePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> SaveImageAsync(IFormFile? file, string subFolder = "complaints")
        {
            if (file == null || file.Length == 0)
            {
                return (true, null, null);
            }

            if (file.Length > MaxFileSize)
            {
                return (false, null, "File size exceeds the 5MB limit.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                return (false, null, "Invalid file format. Allowed formats: .jpg, .jpeg, .png, .webp");
            }

            try
            {
                // Ensure upload directory exists inside wwwroot
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate safe unique filename to avoid path traversal and overwrites
                var uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{extension}";
                var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativeUrl = $"/uploads/{subFolder}/{uniqueFileName}";
                _logger.LogInformation("Saved evidence image to {Path}", relativeUrl);
                return (true, relativeUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);
                return (false, null, "An error occurred while saving the uploaded image.");
            }
        }

        public void DeleteImage(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                // Prevent path traversal
                var cleanPath = relativePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());
                var physicalPath = Path.Combine(_env.WebRootPath, cleanPath);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {Path}", relativePath);
            }
        }
    }
}
