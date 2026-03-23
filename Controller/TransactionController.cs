using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StudentGearHub.Model;
using System.Data.SqlClient;

namespace StudentGearHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly string? _connectionString;

        public TransactionController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // POST: api/Transaction/purchase
        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] TransactionModel transaction)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var checkStock = "SELECT Stock FROM Products WHERE Id = @ProductId";
                using var checkCmd = new SqlCommand(checkStock, conn);
                checkCmd.Parameters.AddWithValue("@ProductId", transaction.ProductId);
                var stock = (int?)await checkCmd.ExecuteScalarAsync();

                if (stock == null || stock < transaction.Quantity)
                    return BadRequest(new { message = "Insufficient stock." });

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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT: api/Transaction/return
        [HttpPut("return")]
        public async Task<IActionResult> Return([FromBody] ReturnModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "UPDATE Transactions SET Status = 'Returned' WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", model.TransactionId);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Item returned successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE: api/Transaction/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var query = "DELETE FROM Transactions WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Transaction deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Transaction/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
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
                return NotFound(new { message = "Transaction not found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Transaction
        [HttpGet]
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }

    public class ReturnModel
    {
        public int TransactionId { get; set; }
    }
}