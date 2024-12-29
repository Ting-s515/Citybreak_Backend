namespace testCitybreak.DTO
{
	public class UserDataDTO
	{
		public int userID { get; set; }
		public string? webMemberID { get; set; }
		public string? name { get; set; }
		public string email { get; set; } = null!;
		public string? phone { get; set; }
		public DateOnly createdDate { get; set; }
		public string? googleName { get; set; }
		public string? googleEmail { get; set; }
	}
}
