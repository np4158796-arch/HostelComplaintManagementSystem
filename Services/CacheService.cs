using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Services
{
    public interface ICacheService
    {
        Task<List<ComplaintCategory>> GetCachedCategoriesAsync();
        void InvalidateCategoriesCache();
        Task<List<HostelBlock>> GetCachedBlocksAsync();
        void InvalidateBlocksCache();
    }

    /// <summary>
    /// Demonstrates In-Memory Caching (IMemoryCache) for infrequently changing reference data,
    /// fulfilling IT5039 syllabus Unit III requirements on Data Caching.
    /// Ensures proper expiration (Sliding/Absolute) and programmatic invalidation.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CacheService> _logger;

        private const string CategoriesCacheKey = "ComplaintCategories_Active";
        private const string BlocksCacheKey = "HostelBlocks_Active";

        public CacheService(
            IMemoryCache memoryCache,
            ApplicationDbContext context,
            ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _context = context;
            _logger = logger;
        }

        public async Task<List<ComplaintCategory>> GetCachedCategoriesAsync()
        {
            if (!_memoryCache.TryGetValue(CategoriesCacheKey, out List<ComplaintCategory>? categories) || categories == null)
            {
                _logger.LogInformation("[Cache Miss] Fetching categories from database...");
                categories = await _context.ComplaintCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Id)
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                    .SetPriority(CacheItemPriority.Normal);

                _memoryCache.Set(CategoriesCacheKey, categories, cacheOptions);
            }
            else
            {
                _logger.LogInformation("[Cache Hit] Retrieved categories from memory cache.");
            }

            return categories;
        }

        public void InvalidateCategoriesCache()
        {
            _logger.LogInformation("[Cache Invalidation] Invalidating cached complaint categories.");
            _memoryCache.Remove(CategoriesCacheKey);
        }

        public async Task<List<HostelBlock>> GetCachedBlocksAsync()
        {
            if (!_memoryCache.TryGetValue(BlocksCacheKey, out List<HostelBlock>? blocks) || blocks == null)
            {
                _logger.LogInformation("[Cache Miss] Fetching hostel blocks from database...");
                blocks = await _context.HostelBlocks
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .Include(b => b.Hostel)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2));

                _memoryCache.Set(BlocksCacheKey, blocks, cacheOptions);
            }
            else
            {
                _logger.LogInformation("[Cache Hit] Retrieved hostel blocks from memory cache.");
            }

            return blocks;
        }

        public void InvalidateBlocksCache()
        {
            _logger.LogInformation("[Cache Invalidation] Invalidating cached hostel blocks.");
            _memoryCache.Remove(BlocksCacheKey);
        }
    }
}
