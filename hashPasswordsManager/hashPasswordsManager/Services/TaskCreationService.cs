using Core.Models;
using Core.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using hashPasswordsManager.Services;
using Core.Interfaces;

namespace hashPasswordsManager.Tasks
{
    public class TaskCreationService
    {
        private readonly ILogger<TaskCreationService> _logger;
        private readonly IHashedPasswordStorage _passwordStorage;
        private readonly IWorkerClient _workerClient;
        private readonly RequestStatusService _statusService;
        private readonly IConfiguration _configuration;

        public TaskCreationService(
            ILogger<TaskCreationService> logger,
            IHashedPasswordStorage passwordStorage,
            IWorkerClient workerClient,
            RequestStatusService statusService,
            IConfiguration configuration)
        {
            _logger = logger;
            _passwordStorage = passwordStorage;
            _workerClient = workerClient;
            _statusService = statusService;
            _configuration = configuration;
        }

        public async Task<TaskCreationResult> CreateTaskAsync(HashInfo hashInfo)
        {
            try
            {
                var workerUrls = GetWorkerUrls();

                if (workerUrls.Count == 0)
                {
                    _logger.LogError("нет доступных воркеров для работы");
                    return TaskCreationResult.Failure("нет доступных воркеров");
                }

                (string requestId, bool needWork) = _passwordStorage.CreateNew(
                    hashInfo.Hash!,
                    workerUrls.Count);

                _logger.LogInformation("ID созданной задачи: {RequestId}", requestId);

                if (needWork)
                {
                    _statusService.RegisterRequest(requestId);
                    await DistributeTasksToWorkers(requestId, hashInfo, workerUrls);
                }

                return TaskCreationResult.Success(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ошибка при создании задачи");
                return TaskCreationResult.Failure(ex.Message);
            }
        }

        private List<string> GetWorkerUrls()
        {
            var workerUrls = new List<string>();
            var workerCount = _configuration.GetValue<int>("Worker_Count");

            if (workerCount > 0)
            {
                for (int i = 1; i <= workerCount; i++)
                {
                    workerUrls.Add($"http://hashcrack-worker-{i}:8080");
                }
            }

            return workerUrls;
        }

        private async Task DistributeTasksToWorkers(
            string requestId,
            HashInfo hashInfo,
            List<string> workerUrls)
        {
            _logger.LogInformation("количество воркеров: {WorkerCount}", workerUrls.Count);

            var alphabet = _configuration["EnglishAlphabet"]
                .Select(c => c.ToString())
                .ToList();

            var tasks = new List<Task>();

            for (int i = 0; i < workerUrls.Count; i++)
            {
                var workerRequest = CreateWorkerRequest(requestId, hashInfo, i, workerUrls.Count, alphabet);
                var task = _workerClient.SendTaskToWorker(workerUrls[i], workerRequest);
                tasks.Add(task);
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при распределении задач воркерам");
            }
        }

        private CrackHashManagerRequest CreateWorkerRequest(
            string requestId,
            HashInfo hashInfo,
            int partNumber,
            int partCount,
            List<string> alphabet)
        {
            return new CrackHashManagerRequest
            {
                RequestId = requestId,
                PartNumber = partNumber,
                PartCount = partCount,
                Hash = hashInfo.Hash!,
                MaxLength = (int)hashInfo.MaxLength!,
                Alphabet = new Alphabet { Symbols = alphabet.ToArray() }
            };
        }
    }

    public class TaskCreationResult
    {
        public bool IsSuccess { get; set; }
        public string RequestId { get; set; }
        public string ErrorMessage { get; set; }

        public static TaskCreationResult Success(string requestId)
        {
            return new TaskCreationResult
            {
                IsSuccess = true,
                RequestId = requestId
            };
        }

        public static TaskCreationResult Failure(string errorMessage)
        {
            return new TaskCreationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}