using System.ComponentModel.DataAnnotations;

namespace GameStop.Web.Models.ViewModels;

public class GameFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Platform { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Genre { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0, 99999)]
    public decimal Price { get; set; }

    [Required, Range(0, 99999)]
    public int StockCount { get; set; }

    public string? ExistingImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
}
