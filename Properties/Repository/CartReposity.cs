using StudentGearHub.API.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StudentGearHub.Properties.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly string _connectionString;

        public CartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<CartResponse> AddToCart(AddToCartRequest request)
        {
            const string checkQuery = @"
                SELECT COUNT(*) FROM Cart 
                WHERE StudentId = @StudentId AND ItemId = @ItemId AND ItemType = @ItemType";

            const string updateQuery = @"
                UPDATE Cart SET Quantity = Quantity + @Quantity
                WHERE StudentId = @StudentId AND ItemId = @ItemId AND ItemType = @ItemType";

            const string insertQuery = @"
                INSERT INTO Cart (StudentId, ItemId, ItemType, Quantity)
                VALUES (@StudentId, @ItemId, @ItemType, @Quantity)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if item already in cart
            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@StudentId", request.StudentId);
            checkCmd.Parameters.AddWithValue("@ItemId", request.ItemId);
            checkCmd.Parameters.AddWithValue("@ItemType", request.ItemType);
            var exists = (int)await checkCmd.ExecuteScalarAsync();

            if (exists > 0)
            {
                // Update quantity
                using var updateCmd = new SqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                updateCmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                updateCmd.Parameters.AddWithValue("@ItemId", request.ItemId);
                updateCmd.Parameters.AddWithValue("@ItemType", request.ItemType);
                await updateCmd.ExecuteNonQueryAsync();

                return new CartResponse { Success = true, Message = "Cart quantity updated." };
            }

            // Insert new cart item
            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@StudentId", request.StudentId);
            insertCmd.Parameters.AddWithValue("@ItemId", request.ItemId);
            insertCmd.Parameters.AddWithValue("@ItemType", request.ItemType);
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
                SELECT 
                    c.CartItemId, c.StudentId, c.ItemId, c.ItemType, c.Quantity,
                    CASE 
                        WHEN c.ItemType = 'Gear' THEN g.ItemName
                        WHEN c.ItemType = 'Uniform' THEN u.ItemName
                    END AS ItemName,
                    CASE 
                        WHEN c.ItemType = 'Gear' THEN g.Price
                        WHEN c.ItemType = 'Uniform' THEN u.Price
                    END AS Price,
                    CASE 
                        WHEN c.ItemType = 'Gear' THEN g.ImageUrl
                        WHEN c.ItemType = 'Uniform' THEN u.ImageUrl
                    END AS ImageUrl
                FROM Cart c
                LEFT JOIN GearItems g ON c.ItemId = g.ItemId AND c.ItemType = 'Gear'
                LEFT JOIN UniformItems u ON c.ItemId = u.ItemId AND c.ItemType = 'Uniform'
                WHERE c.StudentId = @StudentId";

            var cartItems = new List<CartItemResponse>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var price = Convert.ToDecimal(reader["Price"]);
                var quantity = Convert.ToInt32(reader["Quantity"]);

                cartItems.Add(new CartItemResponse
                {
                    CartItemId = Convert.ToInt32(reader["CartItemId"]),
                    StudentId = reader["StudentId"].ToString(),
                    ItemId = Convert.ToInt32(reader["ItemId"]),
                    ItemName = reader["ItemName"].ToString(),
                    ItemType = reader["ItemType"].ToString(),
                    ImageUrl = reader["ImageUrl"].ToString(),
                    Quantity = quantity,
                    Price = price,
                    TotalPrice = price * quantity
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