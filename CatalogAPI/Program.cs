using Models; // Til MongoDB settings
using MongoDB.Driver;
using NLog;
using NLog.Web;

try
{
    var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
        .GetCurrentClassLogger();
    logger.Debug("init main");

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers();

    // Load MongoDB configuration
    builder.Services.Configure<MongoDBSettings>(
        builder.Configuration.GetSection("MongoDB"));

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

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

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
