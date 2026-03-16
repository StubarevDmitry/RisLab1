using Core.Interfaces;
using Core.Model;

namespace hashPasswordsManager.Storages;

public class HashedPasswordStorage : IHashedPasswordStorage
{
    private readonly List<KeyValuePair<Guid, HashedPassword>> _hashedPasswords = new();
    private readonly ILogger<HashedPasswordStorage> _logger;
    private readonly int _maxStorageSize = 1000;
    private readonly object _lockObject = new();

    public HashedPasswordStorage(ILogger<HashedPasswordStorage> logger)
    {
        _logger = logger;
    }

    public (string, bool) CreateNew(string hash, int workerCount)
    {
        lock (_lockObject)
        {
            if (_hashedPasswords.Count >= _maxStorageSize)
            {
                _hashedPasswords.RemoveAt(0);
            }

            foreach (var item in _hashedPasswords)
            {
                if (item.Value.Hash == hash)
                {
                    return (item.Key.ToString(), false);
                }
            }

            Guid id = Guid.NewGuid();
            var hashedPassword = new HashedPassword(hash, workerCount);

            _hashedPasswords.Add(new KeyValuePair<Guid, HashedPassword>(id, hashedPassword));

            _logger.LogDebug($"Создана новая запись с id: {id}");
            return (id.ToString(), true);
        }
    }

    public HashedPassword? GetHashedPassword(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            _logger.LogWarning($"Некорректный формат id: {id}");
            return null;
        }

        lock (_lockObject)
        {
            var item = _hashedPasswords.FirstOrDefault(kvp => kvp.Key == guid);
            return item.Equals(default(KeyValuePair<Guid, HashedPassword>)) ? null : item.Value;
        }
    }

    public void SetWorkerCompleted(string id, int partNumber, string[]? passwords, int taskStatus)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            _logger.LogWarning($"Некорректный формат id: {id}");
            return;
        }

        lock (_lockObject)
        {
            var index = _hashedPasswords.FindIndex(kvp => kvp.Key == guid);

            if (index == -1)
            {
                _logger.LogWarning($"Запись с id {id} не найдена");
                return;
            }

            var item = _hashedPasswords[index];
            var hashedPassword = item.Value;

            if (partNumber >= 0 && partNumber < hashedPassword.WorkerCompleted.Length)
            {
                hashedPassword.WorkerCompleted[partNumber] = taskStatus;

                if (passwords != null && passwords.Length > 0)
                {
                    if (hashedPassword.Passwords == null)
                    {
                        hashedPassword.Passwords = passwords;
                    }
                    else
                    {
                        hashedPassword.Passwords = hashedPassword.Passwords.Concat(passwords).ToArray();
                    }
                }

                _hashedPasswords[index] = new KeyValuePair<Guid, HashedPassword>(guid, hashedPassword);

                _logger.LogDebug($"Обновлен статус воркера {partNumber} для id {id}");
            }
            else
            {
                _logger.LogWarning($"Воркер отправил некорректный номер части: {partNumber}");
            }
        }
    }

    public bool AllWorkersCompleted(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            _logger.LogWarning($"Некорректный формат id: {id}");
            return false;
        }

        lock (_lockObject)
        {
            var item = _hashedPasswords.FirstOrDefault(kvp => kvp.Key == guid);

            if (item.Equals(default(KeyValuePair<Guid, HashedPassword>)))
            {
                _logger.LogWarning($"Запись с id {id} не найдена");
                return false;
            }

            return item.Value.WorkerCompleted.All(completed => 1 == completed);
        }
    }

    public bool PartiallyСompleted(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            _logger.LogWarning($"Некорректный формат id: {id}");
            return false;
        }

        lock (_lockObject)
        {
            var item = _hashedPasswords.FirstOrDefault(kvp => kvp.Key == guid);

            if (item.Equals(default(KeyValuePair<Guid, HashedPassword>)))
            {
                _logger.LogWarning($"Запись с id {id} не найдена");
                return false;
            }

            return item.Value.WorkerCompleted.Any(completed => completed == 2);
        }
    }

    public int GetCurrentStorageSize()
    {
        lock (_lockObject)
        {
            return _hashedPasswords.Count;
        }
    }
    public List<(Guid id, HashedPassword password)> GetAllRecords()
    {
        lock (_lockObject)
        {
            return _hashedPasswords.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }
    }
}