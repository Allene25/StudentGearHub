namespace StudentGearHub.Model
{
    public class TransactionModel
    {
        public int StudentId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReturnModel
    {
        public int TransactionId { get; set; }
    }
}