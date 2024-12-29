namespace testCitybreak.DTO
{
	public class OrderDataDTO
	{
		public short orderID { get; set; }
		public int userID { get; set; }
		public string merchantTradeNo { get; set; } = null!;
		public decimal totalPrice { get; set; }
		public string? orderStatus { get; set; }
		public DateTime orderTime { get; set; }
		public DateTime? latestUpdatedTime { get; set; }
		public byte quantity { get; set; }
		public string? productName { get; set; }
		public string? imagePath { get; set; }
		public short? unitPrice { get; set; }
		public List<Products>? productsData { get; set; }
		public List<OrderDetails>? orderDetails { get; set; }
	}
	public class Products
	{
		public int productID { get; set; }
		public string? productName { get; set; }
		public string? prodictIntroduce { get; set; }
		public short? unitPrice { get; set; }
		public byte? unitStock { get; set; }
		public string? imagePath { get; set; }
		public byte? classificationID { get; set; }
	}
	public class OrderDetails
	{
		public short detailID { get; set; }
		public short orderID { get; set; }
		public int productID { get; set; }
		public byte quantity { get; set; }
	}
}
