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
    public class TransactionController : ControllerBase
    {
        private readonly string? _connectionString;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(IConfiguration configuration, ILogger<TransactionController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // POST: api/Transaction/purchase
        [HttpPost("purchase")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Purchase([FromBody] TransactionModel transaction)
        {
            try
            {
                // Validate input
                if (transaction.StudentId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid student ID.",
                        StatusCode = 400
                    });
                }

                if (transaction.ProductId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid product ID.",
                        StatusCode = 400
                    });
                }

                if (transaction.Quantity <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Quantity must be greater than zero.",
                        StatusCode = 400
                    });
                }

                if (transaction.TotalAmount <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid total amount.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var checkStock = "SELECT Stock FROM Products WHERE Id = @ProductId";
                using var checkCmd = new SqlCommand(checkStock, conn);
                checkCmd.Parameters.AddWithValue("@ProductId", transaction.ProductId);
                var stock = (int?)await checkCmd.ExecuteScalarAsync();

                if (stock == null || stock < transaction.Quantity)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Insufficient stock",
                        StatusCode = 400
                    });
                }

                var insertQuery = @"INSERT INTO Transactions (StudentId, ProductId, Quantity, TotalAmount, Status, TransactionDate)
                                    VALUES (@StudentId, @ProductId, @Quantity, @TotalAmount, 'Pending', GETDATE())";
                using var insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@StudentId", transaction.StudentId);
                insertCmd.Parameters.AddWithValue("@ProductId", transaction.ProductId);
                insertCmd.Parameters.AddWithValue("@Quantity", transaction.Quantity);
                insertCmd.Parameters.AddWithValue("@TotalAmount", transaction.TotalAmount);
                await insertCmd.ExecuteNonQueryAsync();

                var updateStock = "UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId";
                using var updateCmd = new SqlCommand(updateStock, conn);
                updateCmd.Parameters.AddWithValue("@Quantity", transaction.Quantity);
                updateCmd.Parameters.AddWithValue("@ProductId", transaction.ProductId);
                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Purchase successful." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error processing purchase.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing purchase.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // PUT: api/Transaction/return
        [HttpPut("return")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Return([FromBody] ReturnModel model)
        {
            try
            {
                if (model.TransactionId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid transaction ID.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "UPDATE Transactions SET Status = 'Returned' WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", model.TransactionId);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Item returned successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error processing return.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing return.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // DELETE: api/Transaction/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid transaction ID.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "DELETE FROM Transactions WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Transaction deleted successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error deleting transaction.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting transaction.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // GET: api/Transaction/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid transaction ID.",
                        StatusCode = 400
                    });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "SELECT * FROM Transactions WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        Id = reader["Id"],
                        StudentId = reader["StudentId"],
                        ProductId = reader["ProductId"],
                        Quantity = reader["Quantity"],
                        TotalAmount = reader["TotalAmount"],
                        Status = reader["Status"],
                        TransactionDate = reader["TransactionDate"]
                    });
                }
                return NotFound(new ErrorResponse
                {
                    Message = "Transaction not found.",
                    StatusCode = 404
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error retrieving transaction.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving transaction.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        // GET: api/Transaction
        [HttpGet]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var transactions = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "SELECT * FROM Transactions ORDER BY TransactionDate DESC";
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        Id = reader["Id"],
                        StudentId = reader["StudentId"],
                        ProductId = reader["ProductId"],
                        Quantity = reader["Quantity"],
                        TotalAmount = reader["TotalAmount"],
                        Status = reader["Status"],
                        TransactionDate = reader["TransactionDate"]
                    });
                }
                return Ok(transactions);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error retrieving transactions.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving transactions.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }
    }

    public class ReturnModel
    {
        public int TransactionId { get; set; }
    }
}