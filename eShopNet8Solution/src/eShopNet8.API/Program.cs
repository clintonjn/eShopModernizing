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
    .WriteTo.File("logs/eshop-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

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