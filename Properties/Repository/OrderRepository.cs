using StudentGearHub.API.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StudentGearHub.API.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<OrderResponse> Checkout(CheckoutRequest request)
        {
            const string getCartQuery = @"
                SELECT 
                    c.CartItemId, c.ItemId, c.ItemType, c.Quantity,
                    CASE 
                        WHEN c.ItemType = 'Gear' THEN g.Price
                        WHEN c.ItemType = 'Uniform' THEN u.Price
                    END AS Price
                FROM Cart c
                LEFT JOIN GearItems g ON c.ItemId = g.ItemId AND c.ItemType = 'Gear'
                LEFT JOIN UniformItems u ON c.ItemId = u.ItemId AND c.ItemType = 'Uniform'
                WHERE c.StudentId = @StudentId";

            const string insertOrderQuery = @"
                INSERT INTO Orders (StudentId, TotalAmount, PaymentMethod, Notes, Status, OrderDate)
                OUTPUT INSERTED.OrderId
                VALUES (@StudentId, @TotalAmount, @PaymentMethod, @Notes, 'Pending', GETDATE())";

            const string insertOrderItemQuery = @"
                INSERT INTO OrderItems (OrderId, ItemId, ItemType, Quantity, Price)
                VALUES (@OrderId, @ItemId, @ItemType, @Quantity, @Price)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get cart items
                var cartItems = new List<(int ItemId, string ItemType, int Quantity, decimal Price)>();
                decimal totalAmount = 0;

                using var cartCmd = new SqlCommand(getCartQuery, connection, transaction);
                cartCmd.Parameters.AddWithValue("@StudentId", request.StudentId);

                using var reader = await cartCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var price = Convert.ToDecimal(reader["Price"]);
                    var qty = Convert.ToInt32(reader["Quantity"]);
                    cartItems.Add((
                        Convert.ToInt32(reader["ItemId"]),
                        reader["ItemType"].ToString()!,
                        qty,
                        price
                    ));
                    totalAmount += price * qty;
                }
                reader.Close();

                if (cartItems.Count == 0)
                    return new OrderResponse { Success = false, Message = "Cart is empty." };

                // Create order
                using var orderCmd = new SqlCommand(insertOrderQuery, connection, transaction);
                orderCmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                orderCmd.Parameters.AddWithValue("@PaymentMethod", request.PaymentMethod ?? "Cash");
                orderCmd.Parameters.AddWithValue("@Notes", request.Notes ?? "");
                var orderId = (int)await orderCmd.ExecuteScalarAsync();

                // Insert order items
                foreach (var item in cartItems)
                {
                    using var itemCmd = new SqlCommand(insertOrderItemQuery, connection, transaction);
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    itemCmd.Parameters.AddWithValue("@ItemType", item.ItemType);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@Price", item.Price);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                // Clear cart
                using var clearCmd = new SqlCommand("DELETE FROM Cart WHERE StudentId = @StudentId", connection, transaction);
                clearCmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                await clearCmd.ExecuteNonQueryAsync();

                transaction.Commit();

                return new OrderResponse
                {
                    Success = true,
                    Message = "Order placed successfully.",
                    OrderId = orderId,
                    StudentId = request.StudentId,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    OrderDate = DateTime.Now
                };
            }
            catch
            {
                transaction.Rollback();
                return new OrderResponse { Success = false, Message = "Checkout failed. Please try again." };
            }
        }

        public async Task<List<OrderResponse>> GetOrdersByStudent(string studentId)
        {
            const string query = @"
                SELECT OrderId, StudentId, TotalAmount, Status, OrderDate
                FROM Orders
                WHERE StudentId = @StudentId
                ORDER BY OrderDate DESC";

            var orders = new List<OrderResponse>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new OrderResponse
                {
                    Success = true,
                    OrderId = Convert.ToInt32(reader["OrderId"]),
                    StudentId = reader["StudentId"].ToString(),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    Status = reader["Status"].ToString(),
                    OrderDate = Convert.ToDateTime(reader["OrderDate"])
                });
            }

            return orders;
        }

        public async Task<OrderDetailResponse> GetOrderDetail(int orderId)
        {
            const string orderQuery = @"
                SELECT OrderId, StudentId, TotalAmount, Status, PaymentMethod, Notes, OrderDate
                FROM Orders WHERE OrderId = @OrderId";

            const string itemsQuery = @"
                SELECT 
                    oi.ItemId, oi.ItemType, oi.Quantity, oi.Price,
                    CASE 
                        WHEN oi.ItemType = 'Gear' THEN g.ItemName
                        WHEN oi.ItemType = 'Uniform' THEN u.ItemName
                    END AS ItemName
                FROM OrderItems oi
                LEFT JOIN GearItems g ON oi.ItemId = g.ItemId AND oi.ItemType = 'Gear'
                LEFT JOIN UniformItems u ON oi.ItemId = u.ItemId AND oi.ItemType = 'Uniform'
                WHERE oi.OrderId = @OrderId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get order header
            using var orderCmd = new SqlCommand(orderQuery, connection);
            orderCmd.Parameters.AddWithValue("@OrderId", orderId);

            OrderDetailResponse? order = null;
            using var orderReader = await orderCmd.ExecuteReaderAsync();
            if (await orderReader.ReadAsync())
            {
                order = new OrderDetailResponse
                {
                    OrderId = Convert.ToInt32(orderReader["OrderId"]),
                    StudentId = orderReader["StudentId"].ToString(),
                    TotalAmount = Convert.ToDecimal(orderReader["TotalAmount"]),
                    Status = orderReader["Status"].ToString(),
                    PaymentMethod = orderReader["PaymentMethod"].ToString(),
                    Notes = orderReader["Notes"].ToString(),
                    OrderDate = Convert.ToDateTime(orderReader["OrderDate"]),
                    Items = new List<OrderItemDetail>()
                };
            }
            orderReader.Close();

            if (order == null)
                return new OrderDetailResponse();

            // Get order items
            using var itemsCmd = new SqlCommand(itemsQuery, connection);
            itemsCmd.Parameters.AddWithValue("@OrderId", orderId);

            using var itemsReader = await itemsCmd.ExecuteReaderAsync();
            while (await itemsReader.ReadAsync())
            {
                var price = Convert.ToDecimal(itemsReader["Price"]);
                var qty = Convert.ToInt32(itemsReader["Quantity"]);
                order.Items!.Add(new OrderItemDetail
                {
                    ItemId = Convert.ToInt32(itemsReader["ItemId"]),
                    ItemName = itemsReader["ItemName"].ToString(),
                    ItemType = itemsReader["ItemType"].ToString(),
                    Quantity = qty,
                    Price = price,
                    TotalPrice = price * qty
                });
            }

            return order;
        }

        public async Task<OrderResponse> CancelOrder(int orderId)
        {
            const string query = @"
                UPDATE Orders SET Status = 'Cancelled'
                WHERE OrderId = @OrderId AND Status = 'Pending'";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0
                ? new OrderResponse { Success = true, Message = "Order cancelled.", OrderId = orderId, Status = "Cancelled" }
                : new OrderResponse { Success = false, Message = "Order cannot be cancelled. It may already be approved." };
        }
    }
}