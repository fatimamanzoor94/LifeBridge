using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Hubs;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ==================== DATABASE ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==================== SESSION CONFIGURATION ====================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".KhoonEHayat.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ==================== SERVICES ====================
builder.Services.AddHttpClient();

// Notification Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

// External APIs
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

// Profile Service
builder.Services.AddScoped<IProfileService, ProfileService>();

// ==================== MVC & SIGNALR ====================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // ✅ CHANGED: PascalCase (null) se CamelCase mein convert kiya
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddSignalR();

// Configure Authentication Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".KhoonEHayat.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// ==================== LOGGING ====================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ IMPORTANT: Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "profiles");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Console.WriteLine($"Created uploads directory: {uploadsPath}");
}

app.UseHttpsRedirection();

// ✅ Static Files Configuration - CRITICAL for serving uploaded images
app.UseStaticFiles();

// Serve files from uploads folder explicitly
var uploadsFolder = Path.Combine(app.Environment.WebRootPath, "uploads");
if (Directory.Exists(uploadsFolder))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsFolder),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
        {
            // Enable caching for images (optional)
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
        }
    });
}

app.UseRouting();

// ⚠️ Session MUST be between UseRouting and UseAuthorization
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

// Map Controllers with proper areas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ Health check endpoint (optional but recommended)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.Now,
    uploadsFolderExists = Directory.Exists(uploadsPath)
}));

app.Run();

// ==================== HELPER CLASSES (If needed) ====================
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add any additional service registrations here
        return services;
    }
}