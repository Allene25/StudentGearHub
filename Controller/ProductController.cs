using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StudentGearHub.Model;
using StudentGearHub.Models;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly string? _connectionString;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IConfiguration configuration, ILogger<ProductController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // POST: api/Product/Insert
        [HttpPost("Insert")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Insert([FromBody] ProductModel product)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product name is required.",
                        StatusCode = 400
                    });
                }

                if (product.Price < 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product price cannot be negative.",
                        StatusCode = 400
                    });
                }

                if (product.Stock < 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product stock cannot be negative.",
                        StatusCode = 400
                    });
                }

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
                return Ok(new { message = "Product inserted successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error inserting product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // PUT: api/Product/Update
        [HttpPut("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromBody] ProductModel product)
        {
            try
            {
                // Validate input
                if (product.Id <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid product ID.",
                        StatusCode = 400
                    });
                }

                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product name is required.",
                        StatusCode = 400
                    });
                }

                if (product.Price < 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product price cannot be negative.",
                        StatusCode = 400
                    });
                }

                if (product.Stock < 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Product stock cannot be negative.",
                        StatusCode = 400
                    });
                }

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
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error updating product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // DELETE: api/Product/Delete/{id}
        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid product ID.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "DELETE FROM Products WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error deleting product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting product.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // GET: api/Product/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var products = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "SELECT * FROM Products";
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
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error retrieving products.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving products.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // GET: api/Product/GetById/{id}
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid product ID.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "SELECT * FROM Products WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
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
                return NotFound(new ErrorResponse
                {
                    Message = "Product not found.",
                    StatusCode = 404
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error retrieving product by ID.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving product by ID.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }
    }
}