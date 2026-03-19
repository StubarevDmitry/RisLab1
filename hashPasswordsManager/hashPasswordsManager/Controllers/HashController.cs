using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using Core.Models;
using hashPasswordsManager.Tasks;
using hashPasswordsManager.Services;
using Core.Model;
using hashPasswordsManager.Storages;

namespace hashPasswordsManager.Controllers;

[ApiController]
[Route("api/hash")]
public class HashController : ControllerBase
{
    private readonly ILogger<HashController> _logger;
    private readonly RequestStatusService _statusService;
    private readonly TaskCreationService _taskCreationService;
    private readonly HashedPasswordStorage _hashedPasswordStorage;

    public HashController(
        ILogger<HashController> logger,
        RequestStatusService statusService,
        TaskCreationService taskCreationService,
        HashedPasswordStorage hashedPasswordStorage)
    {
        _logger = logger;
        _statusService = statusService;
        _taskCreationService = taskCreationService;
    }

    [HttpPost("crack")]
    public async Task<ActionResult<CrackResponse>> PostHash([FromBody] HashInfo hashInfo)
    {
        try
        {
            var result = await _taskCreationService.CreateTaskAsync(hashInfo);

            if (!result.IsSuccess)
            {
                _logger.LogError("Ошибка создания задачи: {ErrorMessage}", result.ErrorMessage);
                return StatusCode(500, "Internal server error");
            }

            return Ok(new CrackResponse(result.RequestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка при создании задачи");
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
            _hashedPasswordStorage.SetWorkerCompleted(
                workerResponse.RequestId,
                workerResponse.PartNumber,
                workerResponse.Answers?.Words?.Where(x => !x.Equals(string.Empty)).ToArray(),
                1
            );

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении результата от воркера");
            return StatusCode(500, "Internal server error");
        }
    }

    public record CrackResponse(string RequestId);
    public record StatusResponse(string Status, string[]? Data);
}