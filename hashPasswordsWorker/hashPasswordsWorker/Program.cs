using Worker.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddXmlSerializerFormatters();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<HashService>();
        builder.Services.AddSingleton<WorkerTaskQueue>();
        builder.Services.AddHostedService<TaskProcessorService>();

        var app = builder.Build();

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}