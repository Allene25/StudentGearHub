using StudentGearHub.API.IRepository;
using StudentGearHub.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.Properties.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly string? _connectionString;

        public CartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<CartResponse> AddToCart(AddToCartRequest request)
        {
            const string checkQuery = "SELECT COUNT(*) FROM Cart WHERE StudentId = @StudentId AND ProductId = @ProductId";
            const string updateQuery = "UPDATE Cart SET Quantity = Quantity + @Quantity WHERE StudentId = @StudentId AND ProductId = @ProductId";
            const string insertQuery = "INSERT INTO Cart (StudentId, ProductId, Quantity) VALUES (@StudentId, @ProductId, @Quantity)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
            checkCmd.Parameters.AddWithValue("@ProductId", request.ProductId);
            var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);

            if (exists > 0)
            {
                using var updateCmd = new SqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                updateCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
                updateCmd.Parameters.AddWithValue("@ProductId", request.ProductId);
                await updateCmd.ExecuteNonQueryAsync();
                return new CartResponse { Success = true, Message = "Cart quantity updated." };
            }

            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@StudentId", request.StudentId ?? "");
            insertCmd.Parameters.AddWithValue("@ProductId", request.ProductId);
            insertCmd.Parameters.AddWithValue("@Quantity", request.Quantity);
            await insertCmd.ExecuteNonQueryAsync();
            return new CartResponse { Success = true, Message = "Item added to cart." };
        }

        public async Task<CartResponse> RemoveFromCart(int cartItemId)
        {
            const string query = "DELETE FROM Cart WHERE CartItemId = @CartItemId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CartItemId", cartItemId);
            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0
                ? new CartResponse { Success = true, Message = "Item removed from cart." }
                : new CartResponse { Success = false, Message = "Item not found in cart." };
        }

        public async Task<CartResponse> UpdateCartQuantity(int cartItemId, int quantity)
        {
            if (quantity <= 0)
                return await RemoveFromCart(cartItemId);

            const string query = "UPDATE Cart SET Quantity = @Quantity WHERE CartItemId = @CartItemId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@CartItemId", cartItemId);
            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0
                ? new CartResponse { Success = true, Message = "Quantity updated." }
                : new CartResponse { Success = false, Message = "Cart item not found." };
        }

        public async Task<List<CartItemResponse>> GetCartByStudent(string studentId)
        {
            const string query = @"
                SELECT c.CartItemId, c.StudentId, c.ProductId,
                       p.Name AS ProductName, p.Category, p.Price, p.ImageUrl, c.Quantity,
                       (p.Price * c.Quantity) AS TotalPrice
                FROM Cart c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE c.StudentId = @StudentId";

            var cartItems = new List<CartItemResponse>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cartItems.Add(new CartItemResponse
                {
                    CartItemId = Convert.ToInt32(reader["CartItemId"]),
                    StudentId = reader["StudentId"].ToString(),
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = reader["ProductName"].ToString(),
                    Category = reader["Category"].ToString(),
                    ImageUrl = reader["ImageUrl"] == DBNull.Value ? null : reader["ImageUrl"].ToString(),
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Price = Convert.ToDecimal(reader["Price"]),
                    TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                });
            }

            return cartItems;
        }

        public async Task<CartResponse> ClearCart(string studentId)
        {
            const string query = "DELETE FROM Cart WHERE StudentId = @StudentId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);
            await command.ExecuteNonQueryAsync();

            return new CartResponse { Success = true, Message = "Cart cleared." };
        }
    }
}
