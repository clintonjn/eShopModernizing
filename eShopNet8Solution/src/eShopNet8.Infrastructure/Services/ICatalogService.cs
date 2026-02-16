using eShopNet8.Shared.Models;

namespace eShopNet8.Infrastructure.Services;

public interface ICatalogService
{
    Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter = 0, int typeIdFilter = 0, int pageIndex = 0, int pageSize = 10);
    Task<CatalogItem?> GetCatalogItemAsync(int id);
    Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync();
    Task<IEnumerable<CatalogType>> GetCatalogTypesAsync();
    Task<CatalogItem> CreateCatalogItemAsync(CatalogItem catalogItem);
    Task<CatalogItem> UpdateCatalogItemAsync(CatalogItem catalogItem);
    Task<bool> DeleteCatalogItemAsync(int id);
    Task<int> GetCatalogItemsCountAsync(int brandIdFilter = 0, int typeIdFilter = 0);
}