using System.ComponentModel.DataAnnotations;

namespace eShopNet8.Shared.Models;

public class CatalogItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Picture name")]
    [StringLength(255)]
    public string PictureFileName { get; set; } = string.Empty;

    public string PictureUri { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public int CatalogTypeId { get; set; }

    [Display(Name = "Type")]
    public CatalogType? CatalogType { get; set; }

    [Display(Name = "Brand")]
    public int CatalogBrandId { get; set; }

    [Display(Name = "Brand")]
    public CatalogBrand? CatalogBrand { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Stock")]
    public int AvailableStock { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Restock")]
    public int RestockThreshold { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Max stock")]
    public int MaxStockThreshold { get; set; }

    public bool OnReorder { get; set; }

    public string TempImageName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}