using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using StudentGearHub.Model;

namespace StudentGearHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly string? _connectionString;

        public AdminController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // =============================================
        // USER MANAGEMENT
        // =============================================

        // GET: api/Admin/GetAllUsers
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT Id, Username, Email, FullName, CreatedAt FROM Users ORDER BY CreatedAt DESC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new
                    {
                        Id = reader["Id"],
                        Username = reader["Username"],
                        Email = reader["Email"],
                        FullName = reader["FullName"],
                        CreatedAt = reader["CreatedAt"]
                    });
                }
                return Ok(users);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Admin/GetUserById/{id}
        [HttpGet("GetUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT Id, Username, Email, FullName, CreatedAt FROM Users WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        Id = reader["Id"],
                        Username = reader["Username"],
                        Email = reader["Email"],
                        FullName = reader["FullName"],
                        CreatedAt = reader["CreatedAt"]
                    });
                }
                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE: api/Admin/DeleteUser/{id}
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM Users WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "User not found." });
                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =============================================
        // PRODUCT MANAGEMENT
        // =============================================

        // POST: api/Admin/AddProduct
        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromBody] AdminProductModel product)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"INSERT INTO Products (Name, Category, Price, Stock, Description)
                              VALUES (@Name, @Category, @Price, @Stock, @Description)";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", product.Name ?? "");
                cmd.Parameters.AddWithValue("@Category", product.Category ?? "");
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@Stock", product.Stock);
                cmd.Parameters.AddWithValue("@Description", product.Description ?? "");
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Product added successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT: api/Admin/UpdateProduct
        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] AdminProductModel product)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"UPDATE Products SET Name=@Name, Category=@Category,
                              Price=@Price, Stock=@Stock, Description=@Description WHERE Id=@Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", product.Id);
                cmd.Parameters.AddWithValue("@Name", product.Name ?? "");
                cmd.Parameters.AddWithValue("@Category", product.Category ?? "");
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@Stock", product.Stock);
                cmd.Parameters.AddWithValue("@Description", product.Description ?? "");
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Product updated successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE: api/Admin/DeleteProduct/{id}
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM Products WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Product not found." });
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Admin/GetAllProducts
        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT * FROM Products ORDER BY Name", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new
                    {
                        Id = reader["Id"],
                        Name = reader["Name"],
                        Category = reader["Category"],
                        Price = reader["Price"],
                        Stock = reader["Stock"],
                        Description = reader["Description"]
                    });
                }
                return Ok(products);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =============================================
        // TRANSACTION MANAGEMENT
        // =============================================

        // GET: api/Admin/GetAllTransactions
        [HttpGet("GetAllTransactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"SELECT t.Id, t.StudentId, u.FullName AS StudentName,
                              t.ProductId, p.Name AS ProductName,
                              t.Quantity, t.TotalAmount, t.Status, t.TransactionDate
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              LEFT JOIN Users u ON t.StudentId = u.Id
                              ORDER BY t.TransactionDate DESC";
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        Id = reader["Id"],
                        StudentId = reader["StudentId"],
                        StudentName = reader["StudentName"],
                        ProductId = reader["ProductId"],
                        ProductName = reader["ProductName"],
                        Quantity = reader["Quantity"],
                        TotalAmount = reader["TotalAmount"],
                        Status = reader["Status"],
                        TransactionDate = reader["TransactionDate"]
                    });
                }
                return Ok(transactions);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE: api/Admin/DeleteTransaction/{id}
        [HttpDelete("DeleteTransaction/{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM Transactions WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Transaction not found." });
                return Ok(new { message = "Transaction deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =============================================
        // REPORTS
        // =============================================

        // GET: api/Admin/GetSalesSummary
        [HttpGet("GetSalesSummary")]
        public async Task<IActionResult> GetSalesSummary()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"SELECT 
                                COUNT(*) AS TotalTransactions,
                                SUM(TotalAmount) AS TotalSales,
                                COUNT(CASE WHEN Status = 'Paid' THEN 1 END) AS PaidTransactions,
                                COUNT(CASE WHEN Status = 'Pending' THEN 1 END) AS PendingTransactions,
                                COUNT(CASE WHEN Status = 'Returned' THEN 1 END) AS ReturnedTransactions,
                                COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END) AS CancelledTransactions
                              FROM Transactions";
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        TotalTransactions = reader["TotalTransactions"],
                        TotalSales = reader["TotalSales"],
                        PaidTransactions = reader["PaidTransactions"],
                        PendingTransactions = reader["PendingTransactions"],
                        ReturnedTransactions = reader["ReturnedTransactions"],
                        CancelledTransactions = reader["CancelledTransactions"]
                    });
                }
                return Ok(new { message = "No transactions found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Admin/GetInventorySummary
        [HttpGet("GetInventorySummary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"SELECT 
                                COUNT(*) AS TotalProducts,
                                SUM(Stock) AS TotalStock,
                                SUM(Stock * Price) AS TotalInventoryValue,
                                COUNT(CASE WHEN Stock <= 5 THEN 1 END) AS LowStockCount,
                                COUNT(CASE WHEN Stock = 0 THEN 1 END) AS OutOfStockCount
                              FROM Products";
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        TotalProducts = reader["TotalProducts"],
                        TotalStock = reader["TotalStock"],
                        TotalInventoryValue = reader["TotalInventoryValue"],
                        LowStockCount = reader["LowStockCount"],
                        OutOfStockCount = reader["OutOfStockCount"]
                    });
                }
                return Ok(new { message = "No products found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Admin/GetTopSellingProducts
        [HttpGet("GetTopSellingProducts")]
        public async Task<IActionResult> GetTopSellingProducts()
        {
            try
            {
                var products = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = @"SELECT TOP 10 p.Id, p.Name, p.Category,
                              SUM(t.Quantity) AS TotalSold,
                              SUM(t.TotalAmount) AS TotalRevenue
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              WHERE t.Status = 'Paid'
                              GROUP BY p.Id, p.Name, p.Category
                              ORDER BY TotalSold DESC";
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new
                    {
                        Id = reader["Id"],
                        Name = reader["Name"],
                        Category = reader["Category"],
                        TotalSold = reader["TotalSold"],
                        TotalRevenue = reader["TotalRevenue"]
                    });
                }
                return Ok(products);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }

}