using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Worker.Services;

namespace Worker.Controllers;

[ApiController]
[Route("internal/api/worker/hash/crack")]
public class WorkerController : ControllerBase
{
    private readonly ILogger<WorkerController> _logger;
    private readonly WorkerTaskQueue _taskQueue;

    public WorkerController(
        ILogger<WorkerController> logger,
        WorkerTaskQueue taskQueue)
    {
        _logger = logger;
        _taskQueue = taskQueue;
    }

    [HttpPost("task")]
    [Consumes("application/xml")]
    public IActionResult ProcessTask([FromBody] CrackHashManagerRequest request)
    {
        try
        {
            _taskQueue.Enqueue(request);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}