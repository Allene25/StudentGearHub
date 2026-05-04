using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using StudentGearHub.API.IRepository;
using StudentGearHub.API.Model;

namespace StudentGearHub.Controllers
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

            // Generate simple token
            var token = GenerateToken(model.Username!);

            return Ok(new
            {
                message = response.Message,
                token = token,
                username = model.Username,
                expiresAt = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private string GenerateToken(string username)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"]
                            ?? "StudentGearHub@SecretKey1234567890!";

            var payload = $"{username}:{DateTime.UtcNow.AddHours(1):o}";
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);
            var signature = hmac.ComputeHash(payloadBytes);
            var signatureBase64 = Convert.ToBase64String(signature);
            var payloadBase64 = Convert.ToBase64String(payloadBytes);

            return $"{payloadBase64}.{signatureBase64}";
        }
    }
}