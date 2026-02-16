using System.ComponentModel.DataAnnotations;

namespace eShopNet8.Shared.Models;

public class CatalogBrand
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    public ICollection<CatalogItem> CatalogItems { get; set; } = new List<CatalogItem>();
}