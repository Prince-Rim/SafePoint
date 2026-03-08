using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SafePoint_IRS.Data;
using SafePoint_IRS.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using DotNetEnv;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
    envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
    Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddSignalR(); 

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("Fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseSession();

app.MapControllers();

app.MapGet("/api/weather", async (double lat, double lon, IConfiguration configuration) =>
{
    var apiKey = configuration["openweather_api_key"];
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("Weather API key is not configured.", statusCode: StatusCodes.Status500InternalServerError);
    }

    var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return Results.Content(json, "application/json");
    }
    catch (Exception)
    {
        return Results.Problem("Error fetching weather data.", statusCode: StatusCodes.Status500InternalServerError);
    }
}).RequireRateLimiting("Fixed");

app.MapPost("/api/email/send-otp", async (HttpContext context, IConfiguration config, IMemoryCache cache) =>
{
    SendOtpRequest? body;
    try
    {
        body = await context.Request.ReadFromJsonAsync<SendOtpRequest>();
    }
    catch
    {
        return Results.BadRequest();
    }
    if (body?.Email == null || string.IsNullOrWhiteSpace(body.Type))
        return Results.BadRequest();

    var publicKey = config["email_service_public_key"];
    var serviceId = config["email_service_id"];
    var templateId = body.Type == "register" ? config["email_service_template_register"] : config["email_service_template_forgot_password"];
    if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(templateId))
        return Results.Problem("Email service not configured.", statusCode: 500);

    var otp = new Random().Next(100000, 999999);
    cache.Set("otp:" + body.Email, otp.ToString(), TimeSpan.FromMinutes(5));

    var privateKey = config["email_service_private_key"];
    if (string.IsNullOrEmpty(privateKey))
        return Results.Problem("Email service private key not set. Add email_service_private_key to .env.", statusCode: 500);

    var payload = new Dictionary<string, object>
    {
        ["service_id"] = serviceId,
        ["template_id"] = templateId,
        ["user_id"] = publicKey,
        ["template_params"] = new Dictionary<string, object> { ["to_email"] = body.Email, ["passcode"] = otp },
        ["accessToken"] = privateKey
    };

    try
    {
        using var client = new HttpClient();
        var res = await client.PostAsJsonAsync("https://api.emailjs.com/api/v1.0/email/send", payload);
        if (res.IsSuccessStatusCode) return Results.Ok(new { success = true });
        var errorBody = await res.Content.ReadAsStringAsync();
        return Results.Problem(
            detail: $"EmailJS error ({(int)res.StatusCode}): {errorBody}",
            statusCode: 500,
            title: "Failed to send OTP"
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500, title: "Failed to send OTP");
    }
}).RequireRateLimiting("Fixed");

app.MapPost("/api/contact", async (HttpRequest request, IConfiguration config) =>
{
    var formId = config["formspree_form_id"];
    if (string.IsNullOrEmpty(formId))
        return Results.Problem("Contact form not configured.", statusCode: 500);

    var form = await request.ReadFormAsync();
    using var client = new HttpClient();
    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["name"] = form["name"].ToString(),
        ["email"] = form["email"].ToString(),
        ["phone"] = form["phone"].ToString(),
        ["message"] = form["message"].ToString()
    });
    var res = await client.PostAsync($"https://formspree.io/f/{formId}", content);
    if (res.IsSuccessStatusCode)
        return Results.Redirect("/contact.html#sent");
    return Results.StatusCode((int)res.StatusCode);
}).RequireRateLimiting("Fixed");

app.MapGet("/", context =>
{
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

app.MapHub<SafePoint_IRS.Hubs.NotificationHub>("/notificationHub");

app.Run();