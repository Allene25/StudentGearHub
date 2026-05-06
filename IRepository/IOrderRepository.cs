using System.Threading.Tasks;
using System.Collections.Generic;
using StudentGearHub.Model;

namespace StudentGearHub.API.IRepository
{
    public interface IOrderRepository
    {
        Task<OrderResponse> Checkout(CheckoutRequest request);
        Task<List<OrderResponse>> GetOrdersByStudent(string studentId);
        Task<OrderDetailResponse> GetOrderDetail(int orderId);
        Task<OrderResponse> CancelOrder(int orderId);
    }
}