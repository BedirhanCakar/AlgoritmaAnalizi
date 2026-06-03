using ExamScheduler.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ExamScheduler.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseConnStr = configuration.GetConnectionString("DefaultConnection");
    
    var httpContext = httpContextAccessor?.HttpContext;
    if (httpContext == null)
    {
        options.UseSqlServer(baseConnStr);
    }
    else
    {
        string role = "Viewer";
        var user = httpContext.User;
        if (user != null && user.Identity.IsAuthenticated)
        {
            if (user.IsInRole("Admin"))
            {
                role = "Admin";
            }
        }
        
        // Dinamik olarak rol tabanlı bağlantı dizesini oluştur
        var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnStr);
        if (connBuilder.IntegratedSecurity || baseConnStr.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase) || baseConnStr.Contains("Trusted_Connection=yes", StringComparison.OrdinalIgnoreCase))
        {
            connBuilder.IntegratedSecurity = false;
            connBuilder.Remove("Trusted_Connection");
        }
        
        if (role == "Admin")
        {
            connBuilder.UserID = "AppAdminLogin";
            connBuilder.Password = "AdminPassword123!";
        }
        else
        {
            connBuilder.UserID = "AppViewerLogin";
            connBuilder.Password = "ViewerPassword123!";
        }
        
        options.UseSqlServer(connBuilder.ConnectionString);
    }
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

// Session Desteği Ekle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(3); // OTP geçerlilik süresi 3 dakika
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// E-posta Servisi Kaydı
builder.Services.AddTransient<IEmailService, SmtpEmailService>();


var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
