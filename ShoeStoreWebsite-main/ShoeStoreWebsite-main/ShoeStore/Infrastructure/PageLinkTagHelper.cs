using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using ShoeStore.Models.ViewModels;

namespace ShoeStore.Infrastructure;
//Mã nguồn này thuộc namespace ShoeStore.Infrastructure và định nghĩa một lớp có 
//tên PageLinkTagHelper, kế thừa từ TagHelper.Thuộc tính[HtmlTargetElement] 
//chỉ định rằng Tag Helper này áp dụng cho các phần tử<div> có thuộc tính page-model.
//[HtmlTargetElement("div", Attributes = "page-model")]
public class PageLinkTagHelper : TagHelper
{
    private IUrlHelperFactory urlHelperFactory;
    //Lớp có một constructor nhận vào một IUrlHelperFactory làm tham số.
    //Factory này được sử dụng để tạo một phiên bản của IUrlHelper, 
    //được sử dụng sau đó để tạo các URL.
    public PageLinkTagHelper(IUrlHelperFactory helperFactory)
    {
        urlHelperFactory = helperFactory;
    }
    //Các thuộc tính này được sử dụng để lưu trữ thông tin về ngữ cảnh xem hiện tại, 
    //thông tin về phân trang(PageModel), và tên của phương thức hành 
    //động(PageAction) được sử dụng để tạo URL.
    [ViewContext] [HtmlAttributeNotBound] public ViewContext? ViewContext { get; set; }
    public PagingInfo? PageModel { get; set; }
    public string? PageAction { get; set; }
    //Thuộc tính này là một từ điển được sử dụng để lưu trữ các giá trị 
    //bổ sung cho việc tạo URL.Thuộc tính DictionaryAttributePrefix chỉ định 
    //một tiền tố cho các thuộc tính HTML được sử dụng để đặt giá trị trong từ 
    //điển này.
    [HtmlAttributeName(DictionaryAttributePrefix = "page-url-")]
    public Dictionary<string, object> PageUrlValues { get; set; }
        = new Dictionary<string, object>();
    //Các thuộc tính này cho phép tùy chỉnh các lớp CSS để trang trí liên kết trang.
    //PageClassesEnabled xác định liệu có áp dụng các lớp này hay không.
    public bool PageClassesEnabled { get; set; } = false;
    public string PageClass { get; set; } = String.Empty;
    public string PageClassNormal { get; set; } = String.Empty;
    public string PageClassSelected { get; set; } = String.Empty;
    //Phương thức Process được gọi khi Tag Helper được thực thi.Nó tạo HTML cho 
    //các liên kết trang dựa trên thông tin phân trang và ngữ cảnh xem được cung cấp.
    public override void Process(TagHelperContext context,
        TagHelperOutput output)
    {
        if (ViewContext != null && PageModel != null)
        {
            IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
            TagBuilder result = new TagBuilder("div");
            for (int i = 1; i <= PageModel.TotalPages; i++)
            {
                TagBuilder tag = new TagBuilder("a");
                PageUrlValues["page"] = i;
                var request = ViewContext.HttpContext.Request;
                var queryString = HttpUtility.ParseQueryString(request.QueryString.ToString());
                queryString["page"] = i.ToString();
                tag.Attributes["href"] = $"{request.Path}?{queryString.ToString()}";
                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);
                    tag.AddCssClass(i == PageModel.CurrentPage
                        ? PageClassSelected
                        : PageClassNormal);
                }

                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }
            //HTML được tạo ra được thêm vào nội dung của thẻ<div> gốc.
            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}