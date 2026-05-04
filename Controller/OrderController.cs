using StudentGearHub.API.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace StudentGearHub.API.Controllers
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
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                var response = await _orderRepository.Checkout(request);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during checkout.");
                return StatusCode(500, new { message = "A database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetOrders(string studentId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByStudent(studentId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("detail/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderDetail(orderId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order detail.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var response = await _orderRepository.CancelOrder(orderId);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}