using eShopNet8.Infrastructure.Services;
using eShopNet8.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShopNet8.MVC.Controllers;

public class CatalogController : Controller
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? brandFilterApplied, int? typesFilterApplied, int? page)
    {
        try
        {
            const int itemsPage = 10;
            var pageIndex = page ?? 0;

            var catalogItems = await _catalogService.GetCatalogItemsAsync(
                brandFilterApplied ?? 0,
                typesFilterApplied ?? 0,
                pageIndex,
                itemsPage);

            var totalItems = await _catalogService.GetCatalogItemsCountAsync(
                brandFilterApplied ?? 0,
                typesFilterApplied ?? 0);

            var catalogBrands = await _catalogService.GetCatalogBrandsAsync();
            var catalogTypes = await _catalogService.GetCatalogTypesAsync();

            var viewModel = new CatalogIndexViewModel
            {
                CatalogItems = catalogItems,
                CatalogBrands = catalogBrands,
                CatalogTypes = catalogTypes,
                BrandFilterApplied = brandFilterApplied ?? 0,
                TypesFilterApplied = typesFilterApplied ?? 0,
                PaginationInfo = new PaginationInfo
                {
                    ActualPage = pageIndex,
                    ItemsPerPage = itemsPage,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling((decimal)totalItems / itemsPage)
                }
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog index");
            return View("Error");
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var catalogItem = await _catalogService.GetCatalogItemAsync(id);
            if (catalogItem == null)
            {
                return NotFound();
            }

            return View(catalogItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog item details for id {Id}", id);
            return View("Error");
        }
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CatalogItem catalogItem)
    {
        try
        {
            if (ModelState.IsValid)
            {
                await _catalogService.CreateCatalogItemAsync(catalogItem);
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            return View(catalogItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalog item");
            ModelState.AddModelError("", "An error occurred while creating the item.");
            await PopulateDropdownsAsync();
            return View(catalogItem);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var catalogItem = await _catalogService.GetCatalogItemAsync(id);
            if (catalogItem == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync();
            return View(catalogItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog item for edit with id {Id}", id);
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CatalogItem catalogItem)
    {
        try
        {
            if (id != catalogItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _catalogService.UpdateCatalogItemAsync(catalogItem);
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            return View(catalogItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalog item with id {Id}", id);
            ModelState.AddModelError("", "An error occurred while updating the item.");
            await PopulateDropdownsAsync();
            return View(catalogItem);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var catalogItem = await _catalogService.GetCatalogItemAsync(id);
            if (catalogItem == null)
            {
                return NotFound();
            }

            return View(catalogItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog item for delete with id {Id}", id);
            return View("Error");
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var result = await _catalogService.DeleteCatalogItemAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalog item with id {Id}", id);
            return View("Error");
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.CatalogBrands = await _catalogService.GetCatalogBrandsAsync();
        ViewBag.CatalogTypes = await _catalogService.GetCatalogTypesAsync();
    }
}

public class CatalogIndexViewModel
{
    public IEnumerable<CatalogItem> CatalogItems { get; set; } = new List<CatalogItem>();
    public IEnumerable<CatalogBrand> CatalogBrands { get; set; } = new List<CatalogBrand>();
    public IEnumerable<CatalogType> CatalogTypes { get; set; } = new List<CatalogType>();
    public int BrandFilterApplied { get; set; }
    public int TypesFilterApplied { get; set; }
    public PaginationInfo PaginationInfo { get; set; } = new();
}

public class PaginationInfo
{
    public int TotalItems { get; set; }
    public int ItemsPerPage { get; set; }
    public int ActualPage { get; set; }
    public int TotalPages { get; set; }
    public string Previous => (ActualPage == 0) ? "is-disabled" : "";
    public string Next => (ActualPage == TotalPages - 1) ? "is-disabled" : "";
}