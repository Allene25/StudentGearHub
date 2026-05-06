namespace StudentGearHub.Model
{
    public class AddToCartRequest
    {
        public string? StudentId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public string? StudentId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CartResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
