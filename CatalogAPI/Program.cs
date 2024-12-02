using Models; // Til MongoDB settings
using MongoDB.Driver;
using NLog;
using NLog.Web;
using Microsoft.Extensions.FileProviders;
using System.IO;

try
{
    var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
        .GetCurrentClassLogger();
    logger.Debug("init main");

    var builder = WebApplication.CreateBuilder(args);

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
        return database.GetCollection<Models.Product>(settings.CollectionName);
    });

    // Swagger/OpenAPI configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // NLog setup
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add support for serving static files
    builder.Services.AddDirectoryBrowser(); // To browse directories via URL (optional)
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Configure static file serving
    var uploadedImagesPath = "/srv/images";  // Your Docker volume mount path
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadedImagesPath),  // Physical path inside container
        RequestPath = "/UploadedImages"  // URL to access images
    });

    app.UseHttpsRedirection();
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
