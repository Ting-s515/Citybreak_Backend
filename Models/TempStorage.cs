namespace testCitybreak.Models
{
	public static class TempStorage
	{
		private static readonly Dictionary<string, string> _tempData = new Dictionary<string, string>();
		private static readonly object _lock = new object();
		// 存儲
		public static void Store(string key, string value)
		{
			lock (_lock)
			{
				_tempData[key] = value;
			}
		}
		// 獲取
		public static string? GetData(string key)
		{
			lock (_lock)
			{
				_tempData.TryGetValue(key, out var value);
				return value;
			}
		}
		// 刪除
		public static void Remove(string key)
		{
			lock (_lock)
			{
				_tempData.Remove(key);
			}
		}
	}
}
