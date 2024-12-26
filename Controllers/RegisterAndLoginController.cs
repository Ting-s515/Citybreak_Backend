using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using testCitybreak.Models;

namespace testCitybreak.Controllers
{
	public class RegisterAndLoginController : Controller
	{
		private readonly CitybreakContext _context;
		private readonly PasswordHasher<memberTable> _passwordHasher;
		private readonly ILogger<RegisterAndLoginController> _logger;
		public RegisterAndLoginController(CitybreakContext context, ILogger<RegisterAndLoginController> logger)
		{
			_context = context;
			_passwordHasher = new PasswordHasher<memberTable>();
			_logger = logger;
		}
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] memberTable value)
		{
			memberTable? user = await _context.memberTable.FirstOrDefaultAsync
				(u => u.email == value.email);
			if (user != null)
			{
				//比對密碼
				PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword
					(user, user.password, value.password);

				//接受三個參數：用戶實體、存儲的哈希密碼和用戶輸入的密碼
				if (result == PasswordVerificationResult.Success)
				{
					return Ok(new
					{
						success = true,
						Message = "登入成功",
						id = user.userID,
						user.name,
						user.email,
						user.phone,
						user.createdDate,
					});
				}
				else
				{
					return Unauthorized(new
					{
						success = false,
						message = "登入失敗：信箱或密碼錯誤"
					});
				}

			}
			return Unauthorized(new { success = false, message = "用戶不存在" });
		}
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] memberTable value)
		{
			if (_context.memberTable.Any(u => u.email == value.email))
			{
				return Unauthorized(new { success = false, message = "信箱已被註冊" });
			}
			try
			{
				DateOnly date = DateOnly.FromDateTime(DateTime.UtcNow);
				memberTable memberData = new memberTable
				{
					name = value.name,
					email = value.email,
					phone = value.phone,
					createdDate = date,
				};
				//檢查密碼
				if (!string.IsNullOrEmpty(value.password))
				{
					memberData.password = _passwordHasher.HashPassword(null, value.password);
				}
				else
				{
					memberData.password = null;
					memberData.loginFromGoogle = true;
				}
				await _context.memberTable.AddAsync(memberData);
				await _context.SaveChangesAsync();
				return Ok(new
				{
					success = true,
					message = "註冊成功",
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"錯誤: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = $"發生錯誤: {ex.Message}"
				});
			}
		}
		[HttpGet("googleLogin")]
		public IActionResult GoogleLogin()
		{
			var redirectUrl = Url.Action("GoogleResponse", "RegisterAndLogin");
			return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "Google");
		}

		[HttpGet("googleResponse")]
		public async Task<IActionResult> GoogleResponse()
		{
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			string googleName = "", googleEmail = "";
			if (accessToken != null)
			{
				try
				{
					using (var client = new HttpClient())
					{
						client.DefaultRequestHeaders.Authorization = new
							System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
						var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
						if (response.IsSuccessStatusCode)
						{
							var json = await response.Content.ReadAsStringAsync();
							//回應的 JSON 資料
							dynamic userInfo = JsonConvert.DeserializeObject(json);
							googleName = (string)userInfo.name ?? "no name";
							googleEmail = (string)userInfo.email ?? "no email";
						}
					}
					//檢查是否有該會員
					var userExist = await _context.memberTable.Where(x => x.email == googleEmail)
						.Select(x => new { x.userID, x.name, x.email, x.phone }).SingleOrDefaultAsync();

					//前端網址
					string frontendUrl;
					//如果有該筆資料
					if (userExist != null)
					{
						var token = GenerateJwtToken(userExist.userID);
						frontendUrl = $"http://localhost:5173?token={token}&status=success";
						return Redirect(frontendUrl);
					}
					else
					{
						//var token = GenerateJwtToken(googleName, googleEmail);
						string tempIdentifier = Guid.NewGuid().ToString();
						_logger.LogInformation("生成的臨時識別碼: {TempIdentifier}", tempIdentifier);
						//存儲到 TempStorage
						TempStorage.Store(tempIdentifier, JsonConvert.SerializeObject(new
						{
							googleName,
							googleEmail
						}));
						//識別碼作為 Token
						frontendUrl = $"http://localhost:5173?token={tempIdentifier}";
						return Redirect(frontendUrl);
					}
				}
				catch (Exception ex)
				{
					return StatusCode(500, new
					{
						success = false,
						message = "處理過程中發生錯誤",
						error = ex.Message
					});
				}
			}
			else
			{
				return BadRequest(new
				{
					success = false,
					message = "Access Token 不存在"
				});
			}
		}
		//資料庫有資料
		private string GenerateJwtToken(int userID)
		{
			var claims = new List<Claim>
			{
				new Claim("userID", userID.ToString())
			};
			return GenerateJwtToken(claims);
		}

		// 資料庫無資料
		private string GenerateJwtToken(string googleName, string googleEmail)
		{
			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Name, googleName),
				new Claim(JwtRegisteredClaimNames.Email, googleEmail)
			};
			return GenerateJwtToken(claims);
		}
		private string GenerateJwtToken(List<Claim> claims)
		{
			var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");
			if (string.IsNullOrEmpty(secretKey))
			{
				throw new InvalidOperationException("JWT 密鑰未設置");
			}
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				//issuer：表示 Token 的發行者
				issuer: "https://localhost:7130",
				//audience：表示 Token 的接收者
				audience: "http://localhost:5173",
				claims: claims,
				expires: DateTime.Now.AddHours(1),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
		[HttpGet("getDate")]
		public IActionResult getDate()
		{
			DateTime date = DateTime.UtcNow;
			DateOnly dateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
			return Ok(new
			{
				success = true,
				date,
				stringDate = date.ToString("yyyy-MM-dd HH-mm"),
				dateOnly
			});
		}
		[HttpPost("verifyAccount")]
		public async Task<IActionResult> verifyAccount([FromBody] memberTable value)
		{
			memberTable? user = await _context.memberTable
				.Where(x => x.email == value.email && x.phone == value.phone)
			.FirstOrDefaultAsync();
			if (user == null)
			{
				return Unauthorized(new
				{
					success = false,
					message = "驗證失敗: 無此資料"
				});
			}
			else if (user.loginFromGoogle == true)
			{
				return Unauthorized(new
				{
					success = false,
					message = "Google用戶: 請從Google進行登入"
				});
			}
			return Ok(new
			{
				success = true,
				message = "驗證成功",
			});
		}
		[HttpPost("resetPassword")]
		public async Task<IActionResult> resetPassword([FromBody] memberTable value)
		{
			Console.WriteLine($"前端密碼{value.password}");
			try
			{
				memberTable? user = await _context.memberTable.Where(x => x.email == value.email)
				.FirstOrDefaultAsync();
				if (user == null)
				{
					return Unauthorized(new
					{
						success = false,
						message = "驗證失敗"
					});
				}
				var veyifyPassword = _passwordHasher.VerifyHashedPassword(user, user.password, value.password);
				if (veyifyPassword == PasswordVerificationResult.Success)
				{
					return Unauthorized(new
					{
						success = false,
						message = "新密碼不能與舊密碼相同"
					});
				}
				user.password = _passwordHasher.HashPassword(null, value.password);
				await _context.SaveChangesAsync();
				return Ok(new
				{
					success = true,
					message = "修改密碼成功",
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"錯誤類型: {ex.GetType().Name}, 錯誤訊息: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = "伺服器發生錯誤",
					error = ex.Message
				});
			}
		}
		[HttpPost("getGoogleUserInfo")]
		public async Task<IActionResult> GetUserInfo([FromBody] memberTable value)
		{
			_logger.LogInformation("GetUserInfo called with UserID: {UserID}", value.userID);
			if (value.userID.ToString() == null)
			{
				return Unauthorized(new
				{
					success = false,
					message = "參數為空值"
				});
			}
			memberTable? user = await _context.memberTable
				.Where(x => x.userID == value.userID).FirstOrDefaultAsync();
			return Ok(new
			{
				success = true,
				message = "登入成功",
				user.userID,
				user.email,
				user.phone,
				user.name,
			});
		}
		[HttpPost("getTempGoogleUserInfo")]
		public IActionResult GetTempGoogleUserInfo([FromBody] string tempToken)
		{
			_logger.LogInformation("接收到的參數: {TempToken}", tempToken);
			// 從 TempStorage 獲取數據
			string? tempData = TempStorage.GetData(tempToken);
			_logger.LogInformation("暫存數據: {tempData}", tempData);
			if (string.IsNullOrEmpty(tempData))
			{
				_logger.LogWarning("無效的臨時令牌: {TempToken}", tempToken);
				return Unauthorized(new
				{
					success = false,
					message = "無效的臨時令牌"
				});
			}
			var userInfo = JsonConvert.DeserializeObject<dynamic>(tempData);
			// 提取值
			string googleName = (string)userInfo.googleName ?? "no name";
			string googleEmail = (string)userInfo.googleEmail ?? "no email";

			// 記錄到日誌
			_logger.LogInformation("user data: googleName={GoogleName}, googleEmail={GoogleEmail}", googleName, googleEmail);
			// 刪除暫存數據
			TempStorage.Remove(tempToken);
			// 刪除暫存數據
			TempStorage.Remove(tempToken);
			return Ok(new
			{
				success = true,
				googleName,
				googleEmail
			});
		}
	}
}
