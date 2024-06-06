using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Signing;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
namespace ShoeStore.Infrastructure;
public static class SessionExtensions
{
    //SetJson: Đây là một phương thức mở rộng(extension method) cho interface ISession. 
    //Phương thức này được sử dụng để lưu trữ một đối tượng(object) dưới dạng chuỗi 
    //JSON vào phiên làm việc(session). Cụ thể, nó nhận vào một đối tượng session, 
    //một khóa(key) và một giá trị(value) là đối tượng cần lưu trữ.Phương thức sử dụng 
    //JsonSerializer.Serialize để chuyển đối tượng thành chuỗi JSON và sau đó lưu trữ 
    //chuỗi này vào session bằng cách sử dụng SetString của ISession.
    public static void SetJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    //GetJson: Đây cũng là một phương thức mở rộng cho ISession.
    //Phương thức này được sử dụng để lấy đối tượng từ session dựa trên khóa 
    //đã cung cấp.Phương thức trả về một đối tượng có kiểu T (generic), 
    //tức là kiểu được xác định khi phương thức được gọi.Nếu không tìm thấy giá 
    //trị trong session (null), phương thức trả về giá trị mặc định của 
    //kiểu(default(T)), ngược lại nó sử dụng JsonSerializer.Deserialize để 
    //chuyển đổi chuỗi JSON từ session thành đối tượng kiểu T.
    public static T? GetJson<T>(this ISession session, string key)
    {
        var sessionData = session.GetString(key);
        return sessionData == null
        ? default(T)
            : JsonSerializer.Deserialize<T>(sessionData);
    }
}