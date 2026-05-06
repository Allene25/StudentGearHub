using Microsoft.Extensions.Configuration;
using StudentGearHub.API.IRepository;
using StudentGearHub.API.Model;
using Microsoft.Data.SqlClient;

namespace ACLC_Gear_Hub.Properties
{
    public class LoginClass : ILoginRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string? _connectionString;

        public LoginClass(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Get login - validates username and password
        public async Task<ServiceResponse<object>> GetLogin(string username, string password)
        {
            var response = new ServiceResponse<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM Users WHERE Username = @Username AND PasswordHash = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", HashPassword(password));

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                response.IsSuccess = true;
                                response.Message = "Login successful.";
                            }
                            else
                            {
                                response.IsSuccess = false;
                                response.Message = "Invalid username or password.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        // Check if user exists by username and email
        public async Task<bool> UserExists(string username, string email)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username OR Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    return count > 0;
                }
            }
        }

        // Register a new user
        public async Task<ServiceResponse<object>> RegisterUser(User user)
        {
            var response = new ServiceResponse<object>();

            try
            {
                if (await UserExists(user.Username ?? "", user.Email ?? ""))
                {
                    response.IsSuccess = false;
                    response.Message = "Username or email already exists.";
                    return response;
                }

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"INSERT INTO Users (Username, PasswordHash, Email, FullName, CreatedAt)
                                     VALUES (@Username, @PasswordHash, @Email, @FullName, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", user.Username);
                        cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(user.Password ?? ""));
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@FullName", user.FullName);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        response.IsSuccess = rows > 0;
                        response.Message = rows > 0 ? "User registered successfully." : "Registration failed.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        // Hash password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}