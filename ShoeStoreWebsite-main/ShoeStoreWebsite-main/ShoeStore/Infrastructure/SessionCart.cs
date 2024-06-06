using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using ShoeStore.Models;

namespace ShoeStore.Infrastructure;
//Lớp SessionCart được khai báo trong namespace ShoeStore.Infrastructure 
//và mở rộng từ lớp Cart.
public class SessionCart : Cart
{
    //Phương thức này có nhiệm vụ lấy hoặc tạo một đối tượng giỏ hàng liên quan đến 
    //phiên làm việc của người dùng.
    //Nó sử dụng dịch vụ IHttpContextAccessor để truy cập ngữ cảnh HTTP hiện tại và, 
    //sau đó, truy cập phiên làm việc.
    //Nếu một đối tượng giỏ hàng được lưu trữ trong phiên làm việc, 
    //nó được lấy bằng cách sử dụng phân giải JSON. Ngược lại, một đối 
    //tượng SessionCart mới được tạo.
    //Sau đó, phiên làm việc được gán cho thuộc tính Session của đối tượng cart, 
    //và giỏ hàng được trả về.
    public static Cart GetCart(IServiceProvider services)
    {
        ISession? session = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext?.Session;
        SessionCart cart = session?.GetJson<SessionCart>("Cart")
                           ?? new SessionCart();
        cart.Session = session;
        return cart;
    }
    //Thuộc tính này được đánh dấu với[JsonIgnore], chỉ định rằng nó sẽ không 
    //được bao gồm trong quá trình serialization JSON.
    //Nó đại diện cho phiên làm việc liên quan đến giỏ hàng.
    [JsonIgnore] public ISession? Session { get; set; }
    //Các phương thức này ghi đè các phương thức tương ứng từ lớp cơ sở Cart 
    //để thực hiện các thao tác liên quan đến giỏ hàng(thêm, trừ, xóa mục và xóa 
    //toàn bộ giỏ hàng).
    //Sau mỗi thao tác, đối tượng giỏ hàng được serialize và lưu trở lại trong 
    //phiên làm việc của người dùng bằng cách sử dụng phương thức SetJson.
    public override void AddItem(int shoeSizeId, int quantity)
    {
        base.AddItem(shoeSizeId, quantity);
        Session?.SetJson("Cart", this);
    }

    public override void SubtractItem(int shoeSizeId, int quantity)
    {
        base.SubtractItem(shoeSizeId, quantity);
        Session?.SetJson("Cart", this);
    }

    public override void RemoveLine(int shoeSizeId)
    {
        base.RemoveLine(shoeSizeId);
        Session?.SetJson("Cart", this);
    }

    public override void Clear()
    {
        base.Clear();
        Session?.Remove("Cart");
    }
}