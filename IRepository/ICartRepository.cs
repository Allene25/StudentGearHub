using System.Threading.Tasks;
using System.Collections.Generic;

namespace StudentGearHub.API.IRepository
{
    public interface ICartRepository
    {
        Task<CartResponse> AddToCart(AddToCartRequest request);
        Task<CartResponse> RemoveFromCart(int cartItemId);
        Task<CartResponse> UpdateCartQuantity(int cartItemId, int quantity);
        Task<List<CartItemResponse>> GetCartByStudent(string studentId);
        Task<CartResponse> ClearCart(string studentId);
    }
}