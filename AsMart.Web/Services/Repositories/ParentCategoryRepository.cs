using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services.Repositories
{
    public class ParentCategoryRepository : IParentCategoryRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ISlugService _slugService;

        public ParentCategoryRepository(ApplicationDbContext db, ISlugService slugService)
        {
            _db = db;
            _slugService = slugService;
        }

        public async Task<List<ParentCategoryListItemDto>> GetAllAsync()
        {
            var parents = await _db.Categories
                .AsNoTracking()
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return parents.Select(c => new ParentCategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ChildCount = c.Children.Count,
                ProductCount = c.ProductCategories.Count
            }).ToList();
        }

        public async Task<ParentCategoryDetailsDto?> GetDetailsAsync(int id)
        {
            var parent = await _db.Categories
                .AsNoTracking()
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                .FirstOrDefaultAsync(c => c.Id == id && c.ParentCategoryId == null);

            if (parent == null)
                return null;

            return new ParentCategoryDetailsDto
            {
                Id = parent.Id,
                Name = parent.Name,
                Slug = parent.Slug,
                Description = parent.Description,
                DisplayOrder = parent.DisplayOrder,
                IsActive = parent.IsActive,
                ChildCount = parent.Children.Count,
                ProductCount = parent.ProductCategories.Count
            };
        }

        public async Task<ParentCategoryFormDto?> GetForEditAsync(int id)
        {
            var parent = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.ParentCategoryId == null);

            if (parent == null)
                return null;

            return new ParentCategoryFormDto
            {
                Id = parent.Id,
                Name = parent.Name,
                Slug = parent.Slug,
                Description = parent.Description,
                DisplayOrder = parent.DisplayOrder,
                IsActive = parent.IsActive
            };
        }

        public async Task<int> CreateAsync(ParentCategoryFormDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentCategoryId = null,             // TOP LEVEL
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive
            };

            var slugBase = !string.IsNullOrWhiteSpace(dto.Slug)
                ? dto.Slug
                : _slugService.GenerateSlug(dto.Name);

            category.Slug = await GenerateUniqueSlugAsync(slugBase);

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return category.Id;
        }

        public async Task UpdateAsync(ParentCategoryFormDto dto)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("Parent category id is required for update.");

            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.Id.Value && c.ParentCategoryId == null);

            if (category == null)
                throw new InvalidOperationException("Parent category not found.");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;
            category.IsActive = dto.IsActive;
            category.ParentCategoryId = null;   // enforce top-level

            var newSlugBase = !string.IsNullOrWhiteSpace(dto.Slug)
                ? dto.Slug
                : _slugService.GenerateSlug(dto.Name);

            if (!string.Equals(newSlugBase, category.Slug, StringComparison.OrdinalIgnoreCase))
            {
                category.Slug = await GenerateUniqueSlugAsync(newSlugBase, category.Id);
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var parent = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                .FirstOrDefaultAsync(c => c.Id == id && c.ParentCategoryId == null);

            if (parent == null)
                return;

            if (parent.Children.Any())
            {
                throw new InvalidOperationException("Cannot delete parent category with child categories. Reassign or remove children first.");
            }

            if (parent.ProductCategories.Any())
            {
                throw new InvalidOperationException("Cannot delete parent category with products assigned. Reassign or remove products first.");
            }

            _db.Categories.Remove(parent);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _db.Categories.AnyAsync(c => c.Id == id && c.ParentCategoryId == null);
        }

        public async Task<List<Category>> GetAllParentEntitiesAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        private async Task<string> GenerateUniqueSlugAsync(string slugBase, int? excludeId = null)
        {
            var slug = slugBase;
            var originalSlug = slugBase;
            int i = 1;

            while (await _db.Categories.AnyAsync(c =>
                       c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value)))
            {
                slug = $"{originalSlug}-{i++}";
            }

            return slug;
        }
    }
}
