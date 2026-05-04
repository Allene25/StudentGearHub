using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CashierController : ControllerBase
    {
        private readonly string? _connectionString;

        public CashierController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // POST: api/Cashier/ProcessPayment
        [HttpPost("ProcessPayment")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentModel payment)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // Check if transaction exists
                var checkQuery = "SELECT COUNT(1) FROM Transactions WHERE Id = @TransactionId AND Status = 'Pending'";
                using var checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@TransactionId", payment.TransactionId);
                var exists = (int)await checkCmd.ExecuteScalarAsync();

                if (exists == 0)
                    return BadRequest(new { message = "Transaction not found or already processed." });

                // Update transaction status to Paid
                var updateQuery = @"UPDATE Transactions 
                                    SET Status = 'Paid', AmountPaid = @AmountPaid, PaymentDate = GETDATE()
                                    WHERE Id = @TransactionId";
                using var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@TransactionId", payment.TransactionId);
                updateCmd.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Payment processed successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Cashier/GetPendingTransactions
        [HttpGet("GetPendingTransactions")]
        public async Task<IActionResult> GetPendingTransactions()
        {
            try
            {
                var transactions = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT t.Id, t.StudentId, t.ProductId, p.Name AS ProductName,
                              t.Quantity, t.TotalAmount, t.Status, t.TransactionDate
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              WHERE t.Status = 'Pending'
                              ORDER BY t.TransactionDate DESC";

                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        Id = reader["Id"],
                        StudentId = reader["StudentId"],
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

        // GET: api/Cashier/GetAllTransactions
        [HttpGet("GetAllTransactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = new List<object>();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT t.Id, t.StudentId, t.ProductId, p.Name AS ProductName,
                              t.Quantity, t.TotalAmount, t.Status, t.TransactionDate
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              ORDER BY t.TransactionDate DESC";

                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        Id = reader["Id"],
                        StudentId = reader["StudentId"],
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

        // GET: api/Cashier/GetTransactionById/{id}
        [HttpGet("GetTransactionById/{id}")]
        public async Task<IActionResult> GetTransactionById(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT t.Id, t.StudentId, t.ProductId, p.Name AS ProductName,
                              t.Quantity, t.TotalAmount, t.Status, t.TransactionDate
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              WHERE t.Id = @Id";

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
                        ProductName = reader["ProductName"],
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

        // PUT: api/Cashier/CancelTransaction/{id}
        [HttpPut("CancelTransaction/{id}")]
        public async Task<IActionResult> CancelTransaction(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = "UPDATE Transactions SET Status = 'Cancelled' WHERE Id = @Id AND Status = 'Pending'";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return BadRequest(new { message = "Transaction not found or cannot be cancelled." });

                return Ok(new { message = "Transaction cancelled successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET: api/Cashier/GenerateReceipt/{id}
        [HttpGet("GenerateReceipt/{id}")]
        public async Task<IActionResult> GenerateReceipt(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT t.Id, t.StudentId, u.FullName AS StudentName,
                              t.ProductId, p.Name AS ProductName, p.Category,
                              t.Quantity, t.TotalAmount, t.Status, t.TransactionDate
                              FROM Transactions t
                              INNER JOIN Products p ON t.ProductId = p.Id
                              LEFT JOIN Users u ON t.StudentId = u.Id
                              WHERE t.Id = @Id";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        ReceiptNo = reader["Id"],
                        StudentId = reader["StudentId"],
                        StudentName = reader["StudentName"],
                        ProductId = reader["ProductId"],
                        ProductName = reader["ProductName"],
                        Category = reader["Category"],
                        Quantity = reader["Quantity"],
                        TotalAmount = reader["TotalAmount"],
                        Status = reader["Status"],
                        TransactionDate = reader["TransactionDate"],
                        IssuedBy = "ACLC Student Gear Hub"
                    });
                }

                return NotFound(new { message = "Transaction not found." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
    public class PaymentModel
    {
        public int TransactionId { get; set; }
        public decimal AmountPaid { get; set; }
    }
}

