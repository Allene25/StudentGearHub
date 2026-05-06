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
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.StudentId))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Student ID is required.",
                        StatusCode = 400
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Payment method is required.",
                        StatusCode = 400
                    });
                }

                var response = await _orderRepository.Checkout(request);
                return response.Success ? Ok(response) : BadRequest(new ErrorResponse
                {
                    Message = response.Message ?? "Checkout failed.",
                    StatusCode = 400
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error during checkout.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpGet("{studentId}")]
        [Authorize]
        public async Task<IActionResult> GetOrders(string studentId)
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

                var orders = await _orderRepository.GetOrdersByStudent(studentId);
                return Ok(orders);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching orders.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching orders.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpGet("detail/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid order ID.",
                        StatusCode = 400
                    });
                }

                var order = await _orderRepository.GetOrderDetail(orderId);
                return Ok(order);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching order detail.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching order detail.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }

        [HttpPut("cancel/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid order ID.",
                        StatusCode = 400
                    });
                }

                var response = await _orderRepository.CancelOrder(orderId);
                return response.Success ? Ok(response) : BadRequest(new ErrorResponse
                {
                    Message = response.Message ?? "Failed to cancel order.",
                    StatusCode = 400
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error cancelling order.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "A database error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling order.");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = 500
                });
            }
        }
    }
}