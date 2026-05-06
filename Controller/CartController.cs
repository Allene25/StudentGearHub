using StudentGearHub.API.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentGearHub.Model;
using StudentGearHub.Models;
using Microsoft.Data.SqlClient;

namespace StudentGearHub.Controller
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
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var response = await _cartRepository.AddToCart(request);
                
                // Check for insufficient stock error
                if (!response.Success && response.Message?.Contains("stock", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Insufficient stock",
                        StatusCode = 400
                    });
                }
                
                return response.Success ? Ok(response) : BadRequest(new ErrorResponse
                {
                    Message = response.Message ?? "Failed to add item to cart.",
                    StatusCode = 400
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error adding to cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding to cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpDelete("remove/{cartItemId}")]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                if (cartItemId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid cart item ID.",
                        StatusCode = 400
                    });
                }

                var response = await _cartRepository.RemoveFromCart(cartItemId);
                return response.Success ? Ok(response) : NotFound(new ErrorResponse
                {
                    Message = response.Message ?? "Cart item not found.",
                    StatusCode = 404
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error removing cart item.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error removing cart item.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpPut("update/{cartItemId}")]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromQuery] int quantity)
        {
            try
            {
                if (cartItemId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid cart item ID.",
                        StatusCode = 400
                    });
                }

                if (quantity <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Quantity must be greater than zero.",
                        StatusCode = 400
                    });
                }

                var response = await _cartRepository.UpdateCartQuantity(cartItemId, quantity);
                
                // Check for insufficient stock error
                if (!response.Success && response.Message?.Contains("stock", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Insufficient stock",
                        StatusCode = 400
                    });
                }
                
                return response.Success ? Ok(response) : NotFound(new ErrorResponse
                {
                    Message = response.Message ?? "Cart item not found.",
                    StatusCode = 404
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error updating cart quantity.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating cart quantity.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpGet("{studentId}")]
        [Authorize]
        public async Task<IActionResult> GetCart(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Student ID is required.",
                        StatusCode = 400
                    });
                }

                var cartItems = await _cartRepository.GetCartByStudent(studentId);
                return Ok(cartItems);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpDelete("clear/{studentId}")]
        [Authorize]
        public async Task<IActionResult> ClearCart(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Student ID is required.",
                        StatusCode = 400
                    });
                }

                var response = await _cartRepository.ClearCart(studentId);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error clearing cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error clearing cart.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }
    }
}