using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ShoeStore.Models;

public class ShopOrder
{
    public int Id { get; set; }

    // public Guid Code { get; set; }

    public string? ApplicationUserId { get; set; }

    [ForeignKey("ApplicationUserId")]
    [ValidateNever]
    public ApplicationUser? ApplicationUser { get; set; }

    [Required] public DateTime OrderDate { get; set; }
    public DateTime ShippingDate { get; set; }
    public decimal OrderTotal { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }

    public DateTime PaymentDate { get; set; }
    public DateTime PaymentDueDate { get; set; }

    public string? SessionId { get; set; }
    public string? PaymentIntentId { get; set; }

    [Required(ErrorMessage ="Vui lòng nhập họ tên của bạn")] 
    public string Name { get; set; }
    [Required(ErrorMessage ="Vui lòng nhập số điện thoại")]
    [StringLength(12, ErrorMessage = "Số điện thoại yêu cầu là tầm {2} đến {1} chữ số", MinimumLength = 10)]
    [Phone] public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ của bạn")]
    public string Address { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thành phố của bạn")]
    public string City { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên huyện")]
    public string District { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phường")]
    public string Ward { get; set; }

    public string? PostalCode { get; set; }

    [ValidateNever] public ICollection<OrderDetail>? OrderDetails { get; set; }
}