namespace StudentGearHub.Model
{
    public class CheckoutRequest
    {
        public string? StudentId { get; set; }
        public string? PaymentMethod { get; set; } // "Cash", "GCash", etc.
        public string? Notes { get; set; }
    }

    public class OrderResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int OrderId { get; set; }
        public string? StudentId { get; set; }
        public string? Status { get; set; } // "Pending", "Approved", "Cancelled"
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderDetailResponse
    {
        public int OrderId { get; set; }
        public string? StudentId { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemDetail>? Items { get; set; }
    }

    public class OrderItemDetail
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}