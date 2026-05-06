using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustodianController : ControllerBase
    {
        private readonly string? _connectionString;

        public CustodianController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // POST: api/Custodian/AddProduct
        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromBody] CustodianProductModel product)
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

        // PUT: api/Custodian/UpdateProduct
        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] CustodianProductModel product)
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

        // DELETE: api/Custodian/DeleteProduct/{id}
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM Products WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Custodian/GetAllProducts
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

        // GET: api/Custodian/GetProductById/{id}
        [HttpGet("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT * FROM Products WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        Id = reader["Id"],
                        Name = reader["Name"],
                        Category = reader["Category"],
                        Price = reader["Price"],
                        Stock = reader["Stock"],
                        Description = reader["Description"]
                    });
                }
                return NotFound(new { message = "Product not found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT: api/Custodian/UpdateStock
        [HttpPut("UpdateStock")]
        public async Task<IActionResult> UpdateStock([FromBody] StockUpdateModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "UPDATE Products SET Stock = @Stock WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", model.ProductId);
                cmd.Parameters.AddWithValue("@Stock", model.NewStock);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Product not found." });
                return Ok(new { message = "Stock updated successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Custodian/GetLowStockProducts
        [HttpGet("GetLowStockProducts")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            try
            {
                var products = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "SELECT * FROM Products WHERE Stock <= 5 ORDER BY Stock ASC";
                using var cmd = new SqlCommand(query, conn);
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

        // GET: api/Custodian/GetInventorySummary
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
    }
}

   
