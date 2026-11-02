using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Services;
using ShopProject.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using ShopProject.Middleware; // <-- ÄÎÁÀÂËßÅÌ ÄËß ÌÈÄËÂÅĞÀ

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ========== ÍÀÑÒĞÎÉÊÀ IDENTITY ==========
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Âêëş÷àåì ïîäòâåğæäåíèå ïî÷òû
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<RussianIdentityErrorDescriber>() // <--- ÄÎÁÀÂÈÒÜ İÒÓ ÑÒĞÎÊÓ
    .AddDefaultTokenProviders();


// ========== Îãğàíè÷åíèå ïîïûòîê âõîäà ========
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ========== ÍÀÑÒĞÎÉÊÀ ÏÎ×ÒÛ ==========
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// ========== ÑÎÇÄÀÍÈÅ ĞÎËÈ ADMIN ==========
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
}

// ========== HTTP PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");

app.UseRouting();

// ===== ÀÓÒÅÍÒÈÔÈÊÀÖÈß (ÂÀÆÍÎ: ÄÎËÆÍÀ ÁÛÒÜ ÏÅĞÅÄ ÀÂÒÎĞÈÇÀÖÈÅÉ) =====
app.UseAuthentication();
app.UseAuthorization();

// ===== ÌÈÄËÂÅĞ ÄËß ÏĞÎÂÅĞÊÈ ÏÎËÜÇÎÂÀÒÅËß =====
app.UseMiddleware<EnsureUserExistsMiddleware>();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();