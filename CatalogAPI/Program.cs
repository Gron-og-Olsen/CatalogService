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

    // Load MongoDB configuration
    builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));

    // Register MongoDB client and collection
    builder.Services.AddSingleton<IMongoClient>(s =>
    {
        var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDBSettings>();
        return new MongoClient(settings.ConnectionString);
    });

    builder.Services.AddScoped(s =>
    {
        var client = s.GetRequiredService<IMongoClient>();
        var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDBSettings>();
        var database = client.GetDatabase(settings.DatabaseName);
        return database.GetCollection<Product>(settings.CollectionName);
    });
/*
    // Swagger/OpenAPI configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
*/
    // Set up authentication
    var httpClient = new HttpClient { BaseAddress = new Uri("http://authservice") }; // AuthService base URL
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
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = "http://catalogservice",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

    // Add support for serving static files
    builder.Services.AddDirectoryBrowser(); // To browse directories via URL (optional)

    var app = builder.Build();

  /*  // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
*/
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
