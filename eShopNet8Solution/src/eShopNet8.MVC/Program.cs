using eShopNet8.Infrastructure.Data;
using eShopNet8.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Amazon.SimpleSystemsManagement;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/eshop-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure AWS services (only if AWS region is configured)
var awsRegion = builder.Configuration["AWS:Region"];
if (!string.IsNullOrEmpty(awsRegion))
{
    builder.Services.AddSingleton<IAmazonSimpleSystemsManagement>(provider =>
    {
        var config = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
        };
        return new AmazonSimpleSystemsManagementClient(config);
    });
}

// Configure Entity Framework
var connectionString = await GetConnectionStringAsync(builder.Configuration);
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// Register application services
builder.Services.AddScoped<ICatalogService, CatalogService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// Initialize database with retry logic
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    
    await InitializeDatabaseAsync(context, logger);
}

app.Run();

static async Task InitializeDatabaseAsync(CatalogDbContext context, Microsoft.Extensions.Logging.ILogger<Program> logger)
{
    const int maxRetries = 10;
    const int delaySeconds = 5;
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to initialize database (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
            
            await context.Database.EnsureCreatedAsync();
            
            // Seed data if needed
            if (!await context.CatalogTypes.AnyAsync())
            {
                await SeedDataAsync(context);
                logger.LogInformation("Database seeded successfully");
            }
            
            logger.LogInformation("Database initialized successfully");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization attempt {Attempt} failed", i + 1);
            
            if (i == maxRetries - 1)
            {
                logger.LogError(ex, "Database initialization failed after {MaxRetries} attempts", maxRetries);
                throw;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}

static async Task<string> GetConnectionStringAsync(IConfiguration configuration)
{
    // Try to get connection string from AWS Parameter Store first
    var parameterName = configuration["AWS:ParameterStore:ConnectionString"];
    var awsRegion = configuration["AWS:Region"];
    
    if (!string.IsNullOrEmpty(parameterName) && !string.IsNullOrEmpty(awsRegion))
    {
        try
        {
            var config = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
            };
            using var ssmClient = new AmazonSimpleSystemsManagementClient(config);
            var response = await ssmClient.GetParameterAsync(new Amazon.SimpleSystemsManagement.Model.GetParameterRequest
            {
                Name = parameterName,
                WithDecryption = true
            });
            return response.Parameter.Value;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to retrieve connection string from Parameter Store, falling back to configuration");
        }
    }

    // Fallback to configuration
    return configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost,1433;Database=eShopCatalogDb;User Id=sa;Password=Pass@word;Encrypt=True;TrustServerCertificate=True;";
}

static async Task SeedDataAsync(CatalogDbContext context)
{
    // Seed catalog types
    var catalogTypes = new[]
    {
        new eShopNet8.Shared.Models.CatalogType { Type = "Mug" },
        new eShopNet8.Shared.Models.CatalogType { Type = "T-Shirt" },
        new eShopNet8.Shared.Models.CatalogType { Type = "Sheet" },
        new eShopNet8.Shared.Models.CatalogType { Type = "USB Memory Stick" }
    };

    context.CatalogTypes.AddRange(catalogTypes);
    await context.SaveChangesAsync();

    // Seed catalog brands
    var catalogBrands = new[]
    {
        new eShopNet8.Shared.Models.CatalogBrand { Brand = "Azure" },
        new eShopNet8.Shared.Models.CatalogBrand { Brand = ".NET" },
        new eShopNet8.Shared.Models.CatalogBrand { Brand = "Visual Studio" },
        new eShopNet8.Shared.Models.CatalogBrand { Brand = "SQL Server" },
        new eShopNet8.Shared.Models.CatalogBrand { Brand = "Other" }
    };

    context.CatalogBrands.AddRange(catalogBrands);
    await context.SaveChangesAsync();

    // Seed some sample catalog items
    var catalogItems = new[]
    {
        new eShopNet8.Shared.Models.CatalogItem
        {
            Name = ".NET Bot Blue Hoodie",
            Description = "A nice blue hoodie with .NET Bot logo",
            Price = 19.50M,
            PictureFileName = "1.png",
            CatalogTypeId = 2, // T-Shirt
            CatalogBrandId = 2, // .NET
            AvailableStock = 100,
            RestockThreshold = 10,
            MaxStockThreshold = 200
        },
        new eShopNet8.Shared.Models.CatalogItem
        {
            Name = ".NET Black & White Mug",
            Description = "A stylish black and white mug for your morning coffee",
            Price = 8.50M,
            PictureFileName = "2.png",
            CatalogTypeId = 1, // Mug
            CatalogBrandId = 2, // .NET
            AvailableStock = 89,
            RestockThreshold = 5,
            MaxStockThreshold = 100
        }
    };

    context.CatalogItems.AddRange(catalogItems);
    await context.SaveChangesAsync();
}