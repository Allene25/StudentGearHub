using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StudentGearHub.API.IRepository;
using StudentGearHub.API.Model;

namespace StudentGearHub.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginRepository _loginRepo;
        private readonly IConfiguration _configuration;

        public LoginController(ILoginRepository loginRepo, IConfiguration configuration)
        {
            _loginRepo = loginRepo;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _loginRepo.GetLogin(model.Username!, model.Password!);

            if (!response.IsSuccess)
                return Unauthorized(new { message = response.Message });

            // Get user role from response or default to "Student"
            var role = (response.Data as User)?.Role ?? "Student";

            // Generate JWT token
            var token = GenerateJwtToken(model.Username!, role);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return Ok(new
            {
                message = response.Message,
                token,
                username = model.Username,
                role,
                expiresAt = expiresAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private string GenerateJwtToken(string username, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "StudentGearHub@SecretKey1234567890!";
            var issuer = jwtSettings["Issuer"] ?? "StudentGearHub";
            var audience = jwtSettings["Audience"] ?? "StudentGearHubUsers";
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid registration data", errors = ModelState });

            // Check if username already exists
            var existingUser = await _loginRepo.GetLogin(model.Username!, "");
            if (existingUser.IsSuccess)
                return BadRequest(new { message = "Username already exists" });

            // Register the user (you'll need to implement this in your repository)
            // For now, return success
            return Ok(new { message = "Registration successful" });
        }
    }
}