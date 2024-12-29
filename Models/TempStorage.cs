

namespace testCitybreak.Models
{
	public static class TempStorage
	{
		private static readonly Dictionary<string, string> tempDictionary = new Dictionary<string, string>();

		// 儲存
		public static void Store(string key, string value)
		{
			tempDictionary[key] = value;
		}
		// 獲取
		public static string? GetData(string key)
		{
			tempDictionary.TryGetValue(key, out var value);
			return value;
		}
		// 刪除
		public static void Remove(string key)
		{
			tempDictionary.Remove(key);
		}
		public static bool ContainsKey(string key)
		{
			return tempDictionary.ContainsKey(key);
		}

	}
}
