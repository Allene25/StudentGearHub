using StudentGearHub.API.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace StudentGearHub.API.Controllers
{
    [ApiController]
    [Route("cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartRepository cartRepository, ILogger<CartController> logger)
        {
            _cartRepository = cartRepository;
            _logger = logger;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var response = await _cartRepository.AddToCart(request);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error adding to cart.");
                return StatusCode(500, new { message = "A database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding to cart.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var response = await _cartRepository.RemoveFromCart(cartItemId);
                return response.Success ? Ok(response) : NotFound(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("update/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromQuery] int quantity)
        {
            try
            {
                var response = await _cartRepository.UpdateCartQuantity(cartItemId, quantity);
                return response.Success ? Ok(response) : NotFound(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetCart(string studentId)
        {
            try
            {
                var cartItems = await _cartRepository.GetCartByStudent(studentId);
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cart.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpDelete("clear/{studentId}")]
        public async Task<IActionResult> ClearCart(string studentId)
        {
            try
            {
                var response = await _cartRepository.ClearCart(studentId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}