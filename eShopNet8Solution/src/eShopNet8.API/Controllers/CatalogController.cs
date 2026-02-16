using eShopNet8.Infrastructure.Services;
using eShopNet8.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShopNet8.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Get catalog items with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CatalogItemsResponse>> GetCatalogItems(
        [FromQuery] int brandId = 0,
        [FromQuery] int typeId = 0,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var items = await _catalogService.GetCatalogItemsAsync(brandId, typeId, pageIndex, pageSize);
            var totalItems = await _catalogService.GetCatalogItemsCountAsync(brandId, typeId);

            var response = new CatalogItemsResponse
            {
                Data = items,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Count = totalItems
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog items");
            return StatusCode(500, "An error occurred while retrieving catalog items");
        }
    }

    /// <summary>
    /// Get a specific catalog item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogItem>> GetCatalogItem(int id)
    {
        try
        {
            var item = await _catalogService.GetCatalogItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog item with id {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the catalog item");
        }
    }

    /// <summary>
    /// Create a new catalog item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CatalogItem>> CreateCatalogItem([FromBody] CatalogItem catalogItem)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdItem = await _catalogService.CreateCatalogItemAsync(catalogItem);
            return CreatedAtAction(nameof(GetCatalogItem), new { id = createdItem.Id }, createdItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalog item");
            return StatusCode(500, "An error occurred while creating the catalog item");
        }
    }

    /// <summary>
    /// Update an existing catalog item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CatalogItem>> UpdateCatalogItem(int id, [FromBody] CatalogItem catalogItem)
    {
        try
        {
            if (id != catalogItem.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingItem = await _catalogService.GetCatalogItemAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            var updatedItem = await _catalogService.UpdateCatalogItemAsync(catalogItem);
            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalog item with id {Id}", id);
            return StatusCode(500, "An error occurred while updating the catalog item");
        }
    }

    /// <summary>
    /// Delete a catalog item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCatalogItem(int id)
    {
        try
        {
            var result = await _catalogService.DeleteCatalogItemAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalog item with id {Id}", id);
            return StatusCode(500, "An error occurred while deleting the catalog item");
        }
    }

    /// <summary>
    /// Get all catalog brands
    /// </summary>
    [HttpGet("brands")]
    public async Task<ActionResult<IEnumerable<CatalogBrand>>> GetCatalogBrands()
    {
        try
        {
            var brands = await _catalogService.GetCatalogBrandsAsync();
            return Ok(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog brands");
            return StatusCode(500, "An error occurred while retrieving catalog brands");
        }
    }

    /// <summary>
    /// Get all catalog types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<CatalogType>>> GetCatalogTypes()
    {
        try
        {
            var types = await _catalogService.GetCatalogTypesAsync();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog types");
            return StatusCode(500, "An error occurred while retrieving catalog types");
        }
    }
}

public class CatalogItemsResponse
{
    public IEnumerable<CatalogItem> Data { get; set; } = new List<CatalogItem>();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int Count { get; set; }
}