using Models; // Til MongoDB settings
using MongoDB.Driver;
using NLog;
using NLog.Web;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

try
{
    var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
        .GetCurrentClassLogger();
    logger.Debug("init main");

    var builder = WebApplication.CreateBuilder(args);

    // Set up NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllers();

    // Retrieve the MongoDB connection string from the environment variable in docker-compose.yml
    var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
    var VaredatabaseName = Environment.GetEnvironmentVariable("VareDatabaseName");
    var VarecollectionName = Environment.GetEnvironmentVariable("VareCollectionName");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("MongoDB connection string is not set in the environment variables.");
    }

    var mongoSettings = builder.Configuration.GetSection("MongoDB");

    // Register MongoDB services
    builder.Services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(connectionString));
    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(VaredatabaseName);
        return database.GetCollection<Product>(VarecollectionName);
    });

    // Hent AuthService URL fra miljøvariabel
    var authServiceUrl = Environment.GetEnvironmentVariable("AUTHSERVICE_URL")
                         ?? throw new InvalidOperationException("AuthService URL is not configured.");

    // Brug miljøvariabel til at opsætte HttpClient
    var httpClient = new HttpClient { BaseAddress = new Uri(authServiceUrl) };

    // Hent valideringsnøgler fra AuthService
    var authServiceResponse = httpClient.GetAsync("Auth/GetValidationKeys").Result;



    string issuer, secret;

    if (authServiceResponse.IsSuccessStatusCode)
    {
        var keys = authServiceResponse.Content.ReadFromJsonAsync<ValidationKeys>().Result;
        issuer = keys?.Issuer ?? throw new Exception("Issuer not found in AuthService response.");
        secret = keys?.Secret ?? throw new Exception("Secret not found in AuthService response.");
    }
    else
    {
        throw new Exception("Failed to retrieve validation keys from AuthService.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

    // Add support for serving static files
    builder.Services.AddDirectoryBrowser(); // To browse directories via URL (optional)

    var app = builder.Build();


    // Configure static file serving
    var uploadedImagesPath = "/srv/images";  // Your Docker volume mount path
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadedImagesPath),  // Physical path inside container
        RequestPath = "/UploadedImages"  // URL to access images
    });

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    var logger = NLog.LogManager.GetCurrentClassLogger();
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}

// Model for validation keys
public class ValidationKeys
{
    public string Issuer { get; set; }
    public string Secret { get; set; }
}
