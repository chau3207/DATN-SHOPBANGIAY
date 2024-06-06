using NuGet.Packaging.Signing;

namespace ShoeStore.Infrastructure
{
//    public static class UrlExtensions : Định nghĩa một lớp tĩnh (static class)
//    có tên là UrlExtensions.Lớp tĩnh không thể được khởi tạo và chứa các thành viên tĩnh như phương thức mở rộng.
//    public static string PathAndQuery(this HttpRequest request): Đây là một
//    phương thức mở rộng(extension method) cho đối tượng HttpRequest.Phương thức
//    này được gọi như một phần của đối tượng HttpRequest, và nó trả về một
//    chuỗi biểu diễn cho đường dẫn và truy vấn của HTTP request.
//    request.QueryString.HasValue: Kiểm tra xem QueryString có giá trị không.
//? $"{request.Path}{request.QueryString}" : request.Path.ToString();:
//Nếu QueryString có giá trị, thì trả về chuỗi kết hợp giữa Path và QueryString.
//Nếu không, thì trả về đường dẫn dưới dạng chuỗi.
    public static class UrlExtensions
    {
        public static string PathAndQuery(this HttpRequest request) =>
            request.QueryString.HasValue
                ? $"{request.Path}{request.QueryString}"
                : request.Path.ToString();
    }
}