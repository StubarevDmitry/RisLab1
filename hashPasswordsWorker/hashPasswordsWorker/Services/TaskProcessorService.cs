using Core.Models;
using System.Text;
using System.Xml.Serialization;

namespace Worker.Services;

public class TaskProcessorService : BackgroundService
{
    private readonly ILogger<TaskProcessorService> _logger;
    private readonly WorkerTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;

    public TaskProcessorService(
        ILogger<TaskProcessorService> logger,
        WorkerTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _taskQueue.DequeueAsync(stoppingToken);
                if (task != null)
                {
                    await ProcessTaskAsync(task, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ошибка выполнения");
            }
        }
    }

    private async Task ProcessTaskAsync(CrackHashManagerRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var hashService = scope.ServiceProvider.GetRequiredService<HashService>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            _logger.LogInformation("начало работы над" + request.RequestId);

            var alphabet = request.Alphabet?.Symbols?.ToList() ?? new List<string>();

            var results = hashService.FindMatches(
                request.Hash,
                alphabet,
                request.MaxLength,
                request.PartNumber,
                request.PartCount,
                cancellationToken);

            await SendResultsToManager(
                request.RequestId,
                request.PartNumber,
                results,
                httpClientFactory,
                configuration,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "таска оборвалась с ошибкой");
        }
    }

    private async Task SendResultsToManager(
        string requestId,
        int partNumber,
        List<string> results,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var managerUrl = configuration.GetValue<string>("ManagerUrl");

        var response = new CrackHashWorkerResponse
        {
            RequestId = requestId,
            PartNumber = partNumber,
            Answers = new Answers { Words = [.. results.ToArray(), String.Empty] }
        };

        var client = httpClientFactory.CreateClient();
        var serializer = new XmlSerializer(typeof(CrackHashWorkerResponse));

        using var stringWriter = new Utf8StringWriter();
        serializer.Serialize(stringWriter, response);
        var xml = stringWriter.ToString();

        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        _logger.LogInformation("отправка менеджеру");

        var httpResponse = await client.PatchAsync(
            $"{managerUrl}/api/hash/crack/request",
            content,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();
    }
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}