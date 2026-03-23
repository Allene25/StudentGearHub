using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentGearHub.API.IRepository;
using StudentGearHub.API.Model;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace StudentGearHub.Properties.NewFolder
{
    public class RegisterClass
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterClass> _logger;
        private readonly string _connectionString;

        public RegisterClass(IConfiguration configuration, ILogger<RegisterClass> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Register a new user
        public async Task<ServiceResponse> RegisterUserAsync(RegisterModel model)
        {
            var response = new ServiceResponse();

            try
            {
                // Check if user already exists
                if (await UserExistsAsync(model.Username))
                {
                    response.IsSuccess = false;
                    response.Message = "Username already exists.";
                    return response;
                }

                // Hash the password before storing
                string hashedPassword = HashPassword(model.Password);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"INSERT INTO Users (Username, PasswordHash, Email, FullName, CreatedAt)
                                     VALUES (@Username, @PasswordHash, @Email, @FullName, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", model.Username);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            response.IsSuccess = true;
                            response.Message = "User registered successfully.";
                        }
                        else
                        {
                            response.IsSuccess = false;
                            response.Message = "Registration failed. Please try again.";
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during registration for user: {Username}", model.Username);
                response.IsSuccess = false;
                response.Message = "A database error occurred. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for user: {Username}", model.Username);
                response.IsSuccess = false;
                response.Message = "An unexpected error occurred. Please try again later.";
            }

            return response;
        }

        // Check if a username already exists in the database
        private async Task<bool> UserExistsAsync(string username)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        // Simple password hashing using BCrypt pattern (replace with BCrypt.Net in production)
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}