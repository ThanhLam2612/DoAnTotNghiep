namespace DoAnTotNghiep.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount => Quantity * Price; 
    }
}
