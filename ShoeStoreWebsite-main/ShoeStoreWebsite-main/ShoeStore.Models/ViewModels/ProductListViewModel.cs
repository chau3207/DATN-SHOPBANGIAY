using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

namespace ShoeStore.Models.ViewModels;

public class ProductListViewModel
{
    public List<ShoeColor>? ShoeColors { get; set; }
    public IEnumerable<ProductCardViewModel>? ProductCards { get; set; }
    public List<Brand>? Brands { get; set; }
    public List<Size>? Sizes { get; set; }
    public int? SelectedBrandId { get; set; }
    public int? SelectedSizeId { get; set; }
    public decimal? maxPrice { get; set; } = 999999;
    public decimal? minPrice { get; set; } = 0;
    public string? SearchedBrand { get; set; }
    public List<SelectListItem>? SelectListItems { get; set; }
    //public PagingInfo PagingInfo { get; set; } = new();
    // Pagination properties
    public IPagedList<ProductCardViewModel>? PagedProducts { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 12;
    public string? ProductName { get; set; }
    public string CurrentBrand { get; set; }
}