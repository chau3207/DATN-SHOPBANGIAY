using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;

namespace ShoeStore.Models;

public class Size
{
    public int Id { get; set; }
    [Required(ErrorMessage ="Yêu cầu nhập tên đơn vị")] 
    public string Unit { get; set; }
    [Range(1,100,ErrorMessage ="Giá trị yêu cầu là từ 1 đến 100")]
    public double Value { get; set; }

    [JsonIgnore] [ValidateNever] 
    public IEnumerable<ShoeSize>? ShoeSizes { get; set; }
}