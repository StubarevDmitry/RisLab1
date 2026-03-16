using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using Core.Models;
using hashPasswordsManager.Services;
using Core.Model;
using Microsoft.Extensions.Configuration;

namespace hashPasswordsManager.Controllers;

[ApiController]
[Route("api/hash")]
public class HashController : ControllerBase
{
    private readonly ILogger<HashController> _logger;
    private readonly IHashedPasswordStorage _passwordStorage;
    private readonly IWorkerClient _workerClient;
    private readonly RequestStatusService _statusService;
    private readonly IConfiguration _configuration;

    public HashController(
        ILogger<HashController> logger,
        IHashedPasswordStorage passwordStorage,
        RequestStatusService statusService,
        IConfiguration configuration,
        IWorkerClient workerClient)
    {
        _logger = logger;
        _passwordStorage = passwordStorage;
        _statusService = statusService;
        _configuration = configuration;
        _workerClient = workerClient;
    }

    [HttpPost("crack")]
    public async Task<ActionResult<CrackResponse>> PostHash([FromBody] HashInfo hashInfo)
    {
        try
        {
            List<string> workerUrls = new List<string>();
            var workerCount = 0;
            workerCount = _configuration.GetValue<int>("Worker_Count");
            if (workerCount > 0)
            {
                for (int i = 1; i <= workerCount; i++)
                {
                    workerUrls.Add($"http://hashcrack-worker-{i}:8080");
                }
            }

            if (workerUrls.Count == 0)
            {
                _logger.LogError("нет доступных воркеров");
            }

            (string requestId, bool needWork) = _passwordStorage.CreateNew(hashInfo.Hash!, workerUrls.Count);

            _logger.LogInformation("новая таска: " + requestId);

            if (needWork)
            {
                _statusService.RegisterRequest(requestId);

                await DistributeTasksToWorkers(requestId, hashInfo, workerUrls);
            }

            return Ok(new CrackResponse(requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    public ActionResult<StatusResponse> GetStatus([FromQuery] string requestId)
    {
        var (status, data) = _statusService.GetStatus(requestId);

        return status switch
        {
            RequestStatus.NOT_FOUND => NotFound($"Request {requestId} not found"),
            RequestStatus.ERROR => Ok(new StatusResponse("ERROR", null)),
            RequestStatus.IN_PROGRESS => Ok(new StatusResponse("IN_PROGRESS", null)),
            RequestStatus.READY => Ok(new StatusResponse("READY", data)),
            RequestStatus.PARTIALLY_COMPLETED => Ok(new StatusResponse("PARTIALLY_COMPLETED", data)),
            _ => StatusCode(500, "Unexpected status")
        };
    }

    [HttpPatch("crack/request")]
    [Consumes("application/xml")]
    public IActionResult ReceiveWorkerResult([FromBody] CrackHashWorkerResponse workerResponse)
    {
        try
        {
            
            _passwordStorage.SetWorkerCompleted(
                workerResponse.RequestId,
                workerResponse.PartNumber,
                workerResponse.Answers?.Words?.Where(x => !x.Equals(String.Empty)).ToArray(), 1
            );

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task DistributeTasksToWorkers(string requestId, HashInfo hashInfo, List<string> workerUrls)
    {
        _logger.LogInformation("начало отправки тасак рабочим");
        _logger.LogInformation("число рабочих " + workerUrls.Count);

        var alphabet = _configuration["EnglishAlphabet"].Select(c => c.ToString()).ToList();


        var tasks = new List<Task>();

        for (int i = 0; i < workerUrls.Count; i++)
        {
            var workerRequest = new CrackHashManagerRequest
            {
                RequestId = requestId,
                PartNumber = i,
                PartCount = workerUrls.Count,
                Hash = hashInfo.Hash!,
                MaxLength = (int)hashInfo.MaxLength!,
                Alphabet = new Alphabet { Symbols = alphabet.ToArray() }
            };
            var task = _workerClient.SendTaskToWorker(workerUrls[i], workerRequest);
            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public record CrackResponse(string RequestId);

    public record StatusResponse(string Status, string[]? Data);
}