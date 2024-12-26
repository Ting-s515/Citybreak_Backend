using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using testCitybreak.Models;

var builder = WebApplication.CreateBuilder(args);
// �[�� .env ���
Env.Load();
// �K�[�������ҪA��
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
		Console.WriteLine("Google Client ID �� Client Secret ���]�m");
		Environment.Exit(1); // ����ҰʡA�קK���ε{�ǥX��
	}
	googleOptions.ClientId = googleClientId;
	googleOptions.ClientSecret = googleClientSecret;
	var scopes = new[] { "openid", "profile", "email", "https://www.googleapis.com/auth/gmail.send" };
	foreach (var scope in scopes)
	{
		googleOptions.Scope.Add(scope);
	}
	// �ҥΫO�s�O�P
	googleOptions.SaveTokens = true;
	// ���wgoogle api callback path
	googleOptions.CallbackPath = new PathString("/signin-google");
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CitybreakContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("Database")));
// �[�J CORS �䴩
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowSpecificOrigin", policy =>
	{
		//���\�S�w���ӷ�
		policy.WithOrigins("http://localhost:5173", "http://localhost:8080")
		.AllowCredentials() // ���\���ҡ]�p Cookie�^
		.AllowAnyHeader()
		.AllowAnyMethod();
		//���\�Ҧ��ӷ��]�}�o�Ҧ��^
		//builder.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
	});
});
// �K�[ Session �䴩
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	//options.IdleTimeout = TimeSpan.FromMinutes(10); //����
	options.Cookie.HttpOnly = true; // �]�m�� HttpOnly
	options.Cookie.IsEssential = true;
});

//�M���q�{���Ѫ�
builder.Logging.ClearProviders();
//��X��D���x
builder.Logging.AddConsole();
//��X�� Visual Studio Debug ��X����
builder.Logging.AddDebug();
var app = builder.Build();
// �ҥ� CORS
app.UseCors("AllowSpecificOrigin");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//�ҥ�session
app.UseSession();
app.UseAuthorization();


app.MapControllers();

app.Run();
