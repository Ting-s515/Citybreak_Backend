using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using testCitybreak.Models;

var builder = WebApplication.CreateBuilder(args);
// 加載 .env 文件
Env.Load();
// 添加身份驗證服務
builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
	string? googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
	string? googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
	if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
	{
		Console.WriteLine("Google Client ID 或 Client Secret 未設置");
		Environment.Exit(1); // 停止啟動，避免應用程序出錯
	}
	googleOptions.ClientId = googleClientId;
	googleOptions.ClientSecret = googleClientSecret;
	var scopes = new[] { "openid", "profile", "email", "https://www.googleapis.com/auth/gmail.send" };
	foreach (var scope in scopes)
	{
		googleOptions.Scope.Add(scope);
	}
	// 啟用保存令牌
	googleOptions.SaveTokens = true;
	// 指定google api callback path
	googleOptions.CallbackPath = new PathString("/signin-google");
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CitybreakContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("Database")));
// 加入 CORS 支援
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowSpecificOrigin", policy =>
	{
		//允許特定的來源
		policy.WithOrigins("http://localhost:5173", "http://localhost:8080")
		.AllowCredentials() // 允許憑證（如 Cookie）
		.AllowAnyHeader()
		.AllowAnyMethod();
		//允許所有來源（開發模式）
		//builder.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
	});
});
// 添加 Session 支援
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	//options.IdleTimeout = TimeSpan.FromMinutes(10); //期限
	options.Cookie.HttpOnly = true; // 設置為 HttpOnly
	options.Cookie.IsEssential = true;
});

//清除默認提供者
builder.Logging.ClearProviders();
//輸出到主控台
builder.Logging.AddConsole();
//輸出到 Visual Studio Debug 輸出視窗
builder.Logging.AddDebug();
var app = builder.Build();
// 啟用 CORS
app.UseCors("AllowSpecificOrigin");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//啟用session
app.UseSession();
app.UseAuthorization();


app.MapControllers();

app.Run();
