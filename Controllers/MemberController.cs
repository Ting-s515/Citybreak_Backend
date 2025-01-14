using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using testCitybreak.DTO;
using testCitybreak.Models;
namespace testCitybreak.Controllers
{
    public class MemberController : Controller
    {
        private readonly CitybreakContext _context;
        private readonly PasswordHasher<memberTable> _passwordHasher;
        private readonly ILogger<MemberController> _logger;
        public MemberController(CitybreakContext context, ILogger<MemberController> logger)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<memberTable>();
            _logger = logger;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] memberTable value)
        {
            memberTable? member = await _context.memberTable.FirstOrDefaultAsync
                (u => u.email == value.email);
            if (member != null)
            {
                //比對密碼
                PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword
                    (member, member.password, value.password);

                //接受三個參數：用戶實體、存儲的哈希密碼和用戶輸入的密碼
                if (result == PasswordVerificationResult.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        Message = "登入成功",
                        member.userID,
                        member.name,
                        member.email,
                        member.phone,
                        member.createdDate,
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
                _logger.LogError("錯誤={} 堆疊資訊={}", ex.Message, ex);
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
            string? redirectUrl = Url.Action("GoogleResponse", "RegisterAndLogin");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "Google");
        }

        [HttpGet("googleResponse")]
        public async Task<IActionResult> GoogleResponse()
        {
            string? accessToken = await HttpContext.GetTokenAsync("access_token");
            string googleName = "", googleEmail = "";
            if (accessToken != null)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        //jwt
                        client.DefaultRequestHeaders.Authorization = new
                            System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("回傳的json data {}", json);

                            UserDataDTO? userInfo = JsonConvert.DeserializeObject<UserDataDTO>(json);
                            //需對應google回傳的json key
                            googleName = userInfo?.name ?? "no name";
                            googleEmail = userInfo?.email ?? "no email";
                        }
                    }
                    //檢查是否有該會員
                    UserDataDTO? userExist = await _context.memberTable.Where(x => x.email == googleEmail)
                        .Select(x => new UserDataDTO
                        {
                            userID = x.userID,
                            name = x.name,
                            email = x.email,
                            phone = x.phone
                        }).SingleOrDefaultAsync();

                    //前端網址
                    string frontendUrl;
                    //key
                    string tempIdentifier = Guid.NewGuid().ToString();
                    _logger.LogInformation("生成的臨時識別碼: {TempIdentifier}", tempIdentifier);
                    //如果有該筆資料
                    if (userExist != null)
                    {
                        TempStorage.Store(tempIdentifier, JsonConvert.SerializeObject(userExist));
                        frontendUrl = $"http://localhost:5173?token={tempIdentifier}&status=success";
                        return Redirect(frontendUrl);
                    }
                    else
                    {
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
        //測試格式
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
            _logger.LogInformation("前端密碼={} ", value.password);
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
                _logger.LogError("錯誤={} 堆疊資訊={}", ex.Message, ex);
                return StatusCode(500, new
                {
                    success = false,
                    message = "伺服器發生錯誤",
                    error = ex.Message
                });
            }
        }
        [HttpPost("getGoogleUserInfo")]
        public IActionResult GetUserInfo([FromBody] string tempToken)
        {
            _logger.LogInformation("接收到的參數: {TempToken}", tempToken);
            if (!TempStorage.ContainsKey(tempToken))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "無效的token"
                });
            }
            string? tempData = TempStorage.GetData(tempToken);
            _logger.LogInformation("暫存的數據{}", tempData);
            UserDataDTO? userInfo = JsonConvert.DeserializeObject<UserDataDTO>(tempData);
            string userID = userInfo?.userID.ToString() ?? "no id";
            string email = userInfo?.email ?? "no email";
            string name = userInfo?.name ?? "no name";
            string phone = userInfo?.phone ?? "no phone";
            //刪除暫存
            TempStorage.Remove(tempToken);
            return Ok(new
            {
                success = true,
                message = "登入成功",
                userID,
                email,
                name,
                phone
            });
        }
        [HttpPost("getTempGoogleUserInfo")]
        public IActionResult GetTempGoogleUserInfo([FromBody] string tempToken)
        {
            _logger.LogInformation("接收到的參數: {TempToken}", tempToken);
            //uuid != token
            if (!TempStorage.ContainsKey(tempToken))
            {
                _logger.LogWarning("無效的token: {TempToken}", tempToken);
                return Unauthorized(new
                {
                    success = false,
                    message = "無效的token"
                });
            }
            // 從 TempStorage 獲取數據
            string? tempData = TempStorage.GetData(tempToken);
            _logger.LogInformation("暫存數據: {tempData}", tempData);
            UserDataDTO? userInfo = JsonConvert.DeserializeObject<UserDataDTO>(tempData);
            // 提取值
            string googleName = userInfo?.googleName ?? "no name";
            string googleEmail = userInfo?.googleEmail ?? "no email";

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
