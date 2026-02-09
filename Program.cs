// Bring EF Core types into scope only when EF_PRESENT is defined.
#if EF_PRESENT
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Runtime flag to indicate whether EF Core was compiled into the app
var efEnabled = false;
var automapperEnabled = false;

// Add services to the container.

builder.Services.AddControllers();
// Configure EF Core DbContext (SQLite)
// Define EF_PRESENT when restoring/building with EF Core packages to enable the following registration.
#if EF_PRESENT
efEnabled = true;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
// Choose provider based on connection string content: prefer SQL Server when it looks like an Azure/SQL Server connection string
if (connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("Authentication=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<MyEcom.Data.ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<MyEcom.Data.ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
    // Add Identity
    builder.Services.AddIdentity<MyEcom.Data.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
        .AddEntityFrameworkStores<MyEcom.Data.ApplicationDbContext>();
#endif
// AutoMapper registration
automapperEnabled = true;
builder.Services.AddAutoMapper(typeof(MyEcom.Mapping.AutoMapperProfile));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// Always enable Swagger so the app runs on Swagger UI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// Log EF status at startup
if (efEnabled)
{
    app.Logger.LogInformation("Entity Framework Core is enabled and ApplicationDbContext has been registered.");
}
else
{
    app.Logger.LogWarning("Entity Framework Core is NOT enabled. Define 'EnableEf' and restore EF packages to enable DbContext functionality.");
}

// Seed admin user and roles if EF is enabled
#if EF_PRESENT
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MyEcom.Data.ApplicationUser>>();

    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
    }

    var adminEmail = "admin@local";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new MyEcom.Data.ApplicationUser { UserName = "admin", Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(admin, "Admin123$");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
#endif

// Configure the HTTP request pipeline.
// Always expose Swagger UI so the app can be inspected via Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
