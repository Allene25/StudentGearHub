using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentGearHub.API.Model;
using StudentGearHub.Properties.NewFolder;
using System.Threading.Tasks;

namespace StudentGearHub.NewFolder
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly RegisterClass _registerClass;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(RegisterClass registerClass, ILogger<RegisterController> logger)
        {
            _registerClass = registerClass;
            _logger = logger;
        }

        // POST: api/Register
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration model received.");
                return BadRequest(ModelState);
            }

            var response = await _registerClass.RegisterUserAsync(model);

            if (response.IsSuccess)
            {
                _logger.LogInformation("User '{Username}' registered successfully.", model.Username);
                return Ok(new { message = response.Message });
            }
            else
            {
                _logger.LogWarning("Registration failed for user '{Username}': {Message}", model.Username, response.Message);
                return BadRequest(new { message = response.Message });
            }
        }

        // GET: api/Register/check-username/{username}
        [HttpGet("check-username/{username}")]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username cannot be empty." });

            // Create a dummy model to reuse RegisterClass check
            var testModel = new RegisterModel
            {
                Username = username,
                Password = "132123",
                ConfirmPassword = "123123",
                Email = "Bwarwar@gmail.com",
                FullName = "Bwarwar"
            };

            var response = await _registerClass.RegisterUserAsync(testModel);

            bool isAvailable = response.Message != "Username already exists.";
            return Ok(new { username, isAvailable });
        }
    }
}