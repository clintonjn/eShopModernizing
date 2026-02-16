using eShopNet8.Infrastructure.Data;
using eShopNet8.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eShopNet8.Infrastructure.Services;

public class CatalogService : ICatalogService
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(CatalogDbContext context, ILogger<CatalogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter = 0, int typeIdFilter = 0, int pageIndex = 0, int pageSize = 10)
    {
        try
        {
            var query = _context.CatalogItems
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .AsQueryable();

            if (brandIdFilter > 0)
            {
                query = query.Where(c => c.CatalogBrandId == brandIdFilter);
            }

            if (typeIdFilter > 0)
            {
                query = query.Where(c => c.CatalogTypeId == typeIdFilter);
            }

            return await query
                .OrderBy(c => c.Name)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog items");
            throw;
        }
    }

    public async Task<CatalogItem?> GetCatalogItemAsync(int id)
    {
        try
        {
            return await _context.CatalogItems
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog item with id {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync()
    {
        try
        {
            return await _context.CatalogBrands
                .OrderBy(b => b.Brand)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog brands");
            throw;
        }
    }

    public async Task<IEnumerable<CatalogType>> GetCatalogTypesAsync()
    {
        try
        {
            return await _context.CatalogTypes
                .OrderBy(t => t.Type)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog types");
            throw;
        }
    }

    public async Task<CatalogItem> CreateCatalogItemAsync(CatalogItem catalogItem)
    {
        try
        {
            catalogItem.CreatedDate = DateTime.UtcNow;
            _context.CatalogItems.Add(catalogItem);
            await _context.SaveChangesAsync();
            return catalogItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalog item");
            throw;
        }
    }

    public async Task<CatalogItem> UpdateCatalogItemAsync(CatalogItem catalogItem)
    {
        try
        {
            catalogItem.UpdatedDate = DateTime.UtcNow;
            _context.Entry(catalogItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return catalogItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalog item with id {Id}", catalogItem.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCatalogItemAsync(int id)
    {
        try
        {
            var catalogItem = await _context.CatalogItems.FindAsync(id);
            if (catalogItem == null)
            {
                return false;
            }

            _context.CatalogItems.Remove(catalogItem);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalog item with id {Id}", id);
            throw;
        }
    }

    public async Task<int> GetCatalogItemsCountAsync(int brandIdFilter = 0, int typeIdFilter = 0)
    {
        try
        {
            var query = _context.CatalogItems.AsQueryable();

            if (brandIdFilter > 0)
            {
                query = query.Where(c => c.CatalogBrandId == brandIdFilter);
            }

            if (typeIdFilter > 0)
            {
                query = query.Where(c => c.CatalogTypeId == typeIdFilter);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting catalog items");
            throw;
        }
    }
}