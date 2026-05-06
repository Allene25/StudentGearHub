using StudentGearHub.API.IRepository;
using StudentGearHub.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.API.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string? _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<OrderResponse> Checkout(CheckoutRequest request)
        {
            const string checkCartQuery = "SELECT COUNT(*) FROM Cart WHERE StudentId = @StudentId";
            const string calcTotalQuery = @"
                SELECT SUM(p.Price * c.Quantity)
                FROM Cart c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE c.StudentId = @StudentId";
            const string insertOrderQuery = @"
                INSERT INTO Orders (StudentId, TotalAmount, PaymentMethod, Notes, Status, OrderDate)
                OUTPUT INSERTED.OrderId
                VALUES (@StudentId, @TotalAmount, @PaymentMethod, @Notes, 'Pending', GETDATE())";
            const string insertOrderItemsQuery = @"
                INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
                SELECT @OrderId, c.ProductId, c.Quantity, p.Price
                FROM Cart c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE c.StudentId = @StudentId";
            const string deductStockQuery = @"
                UPDATE p SET p.Stock = p.Stock - c.Quantity
                FROM Products p
                INNER JOIN Cart c ON p.Id = c.ProductId
                WHERE c.StudentId = @StudentId";
            const string clearCartQuery = "DELETE FROM Cart WHERE StudentId = @StudentId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Check cart is not empty
                using var checkCmd = new SqlCommand(checkCartQuery, connection, transaction);
                checkCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                var cartCount = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);

                if (cartCount == 0)
                    return new OrderResponse { Success = false, Message = "Cart is empty." };

                // Calculate total
                using var totalCmd = new SqlCommand(calcTotalQuery, connection, transaction);
                totalCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                var totalAmount = Convert.ToDecimal(await totalCmd.ExecuteScalarAsync() ?? 0);

                // Create order
                using var orderCmd = new SqlCommand(insertOrderQuery, connection, transaction);
                orderCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                orderCmd.Parameters.AddWithValue("@PaymentMethod", request.PaymentMethod ?? "Cash");
                orderCmd.Parameters.AddWithValue("@Notes", request.Notes ?? "");
                var orderId = (int)(await orderCmd.ExecuteScalarAsync() ?? 0);

                // Insert order items
                using var itemsCmd = new SqlCommand(insertOrderItemsQuery, connection, transaction);
                itemsCmd.Parameters.AddWithValue("@OrderId", orderId);
                itemsCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                await itemsCmd.ExecuteNonQueryAsync();

                // Deduct stock
                using var stockCmd = new SqlCommand(deductStockQuery, connection, transaction);
                stockCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                await stockCmd.ExecuteNonQueryAsync();

                // Clear cart
                using var clearCmd = new SqlCommand(clearCartQuery, connection, transaction);
                clearCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
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
                SELECT oi.OrderItemId, oi.ProductId, p.Name AS ProductName,
                       p.Category, oi.Quantity, oi.Price,
                       (oi.Price * oi.Quantity) AS TotalPrice
                FROM OrderItems oi
                INNER JOIN Products p ON oi.ProductId = p.Id
                WHERE oi.OrderId = @OrderId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            OrderDetailResponse? order = null;

            using var orderCmd = new SqlCommand(orderQuery, connection);
            orderCmd.Parameters.AddWithValue("@OrderId", orderId);

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
                    Notes = orderReader["Notes"] == DBNull.Value ? null : orderReader["Notes"].ToString(),
                    OrderDate = Convert.ToDateTime(orderReader["OrderDate"]),
                    Items = new List<OrderItemDetail>()
                };
            }
            orderReader.Close();

            if (order == null)
                return new OrderDetailResponse();

            using var itemsCmd = new SqlCommand(itemsQuery, connection);
            itemsCmd.Parameters.AddWithValue("@OrderId", orderId);

            using var itemsReader = await itemsCmd.ExecuteReaderAsync();
            while (await itemsReader.ReadAsync())
            {
                order.Items!.Add(new OrderItemDetail
                {
                    ItemId = Convert.ToInt32(itemsReader["ProductId"]),
                    ItemName = itemsReader["ProductName"].ToString(),
                    ItemType = itemsReader["Category"].ToString(),
                    Quantity = Convert.ToInt32(itemsReader["Quantity"]),
                    Price = Convert.ToDecimal(itemsReader["Price"]),
                    TotalPrice = Convert.ToDecimal(itemsReader["TotalPrice"])
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
