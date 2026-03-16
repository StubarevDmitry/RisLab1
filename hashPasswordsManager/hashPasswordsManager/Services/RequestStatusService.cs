using Core.Interfaces;
using Core.Model;

namespace hashPasswordsManager.Services;

public enum RequestStatus
{
    IN_PROGRESS,
    READY,
    ERROR,
    NOT_FOUND,
    PARTIALLY_COMPLETED
}

public class RequestStatusService
{
    private readonly IHashedPasswordStorage _storage;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, DateTime> _requestStartTimes = new();

    public RequestStatusService(IHashedPasswordStorage storage)
    {
        _storage = storage;
    }

    public void RegisterRequest(string requestId)
    {
        lock (_requestStartTimes)
        {
            _requestStartTimes[requestId] = DateTime.UtcNow;
        }
    }

    public (RequestStatus status, string[]? data) GetStatus(string requestId)
    {
        var hashedPassword = _storage.GetHashedPassword(requestId);

        if (hashedPassword == null)
        {
            return (RequestStatus.NOT_FOUND, null);
        }

        if (IsTimeoutExpired(requestId))
        {
            return (RequestStatus.ERROR, null);
        }

        if (_storage.AllWorkersCompleted(requestId))
        {
            return (RequestStatus.READY, hashedPassword.Passwords ?? Array.Empty<string>());
        }

        if (_storage.PartiallyСompleted(requestId))
        {
            return (RequestStatus.PARTIALLY_COMPLETED, hashedPassword.Passwords ?? Array.Empty<string>());
        }

        return (RequestStatus.IN_PROGRESS, null);
    }

    private bool IsTimeoutExpired(string requestId)
    {
        lock (_requestStartTimes)
        {
            if (_requestStartTimes.TryGetValue(requestId, out var startTime))
            {
                return (DateTime.UtcNow - startTime) > _timeout;
            }
            return false;
        }
    }

    public void CleanupOldRequests()
    {
        lock (_requestStartTimes)
        {
            var expiredKeys = _requestStartTimes
                .Where(kvp => (DateTime.UtcNow - kvp.Value) > _timeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _requestStartTimes.Remove(key);
            }
        }
    }
}