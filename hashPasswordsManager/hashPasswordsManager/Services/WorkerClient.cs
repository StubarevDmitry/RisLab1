namespace hashPasswordsManager.Services
{
    using System.Text;
    using System.Xml.Serialization;
    using Core.Interfaces;
    using Core.Models;
    using hashPasswordsManager.Storages;

    public interface IWorkerClient
    {
        Task SendTaskToWorker(string workerUrl, CrackHashManagerRequest request);
    }

    public class WorkerClient : IWorkerClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WorkerClient> _logger;
        private readonly IHashedPasswordStorage _hashedPasswordStorage;

        public WorkerClient(IHttpClientFactory httpClientFactory, ILogger<WorkerClient> logger, IHashedPasswordStorage hashedPasswordStorage)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hashedPasswordStorage = hashedPasswordStorage;
        }

        public async Task SendTaskToWorker(string workerUrl, CrackHashManagerRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var serializer = new XmlSerializer(typeof(CrackHashManagerRequest));
                using var stringWriter = new Utf8StringWriter();
                serializer.Serialize(stringWriter, request);
                var xml = stringWriter.ToString();


                var content = new StringContent(xml, Encoding.UTF8, "application/xml");


                var response = await client.PostAsync(
                    $"{workerUrl}/internal/api/worker/hash/crack/task",
                    content);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"{workerUrl}: задача успешно отправлена для RequestId={request.RequestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workerUrl}: ошибка при отправке задачи для RequestId={request.RequestId}");
                
                _hashedPasswordStorage.SetWorkerCompleted(request.RequestId, request.PartNumber, [], 2);
            }
        }
    }
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
