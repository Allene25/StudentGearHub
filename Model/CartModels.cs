namespace StudentGearHub.API.IRepository
{
    public class AddToCartRequest
    {
        public string? StudentId { get; set; }
        public int ItemId { get; set; }
        public string? ItemType { get; set; } // "Gear" or "Uniform"
        public int Quantity { get; set; }
    }

    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public string? StudentId { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
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