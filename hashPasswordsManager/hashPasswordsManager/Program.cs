using hashPasswordsManager.Storages;
using hashPasswordsManager.Services;
using Core.Interfaces;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        builder.Services.AddControllers()
            .AddXmlSerializerFormatters()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpClient();


        builder.Services.AddSingleton<IHashedPasswordStorage, HashedPasswordStorage>();
        builder.Services.AddSingleton<RequestStatusService>();
        builder.Services.AddSingleton<IWorkerClient, WorkerClient>();

        var app = builder.Build();

        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.MapControllers();


        var statusService = app.Services.GetRequiredService<RequestStatusService>();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                statusService.CleanupOldRequests();
            }
        });

        app.Run();
    }
}