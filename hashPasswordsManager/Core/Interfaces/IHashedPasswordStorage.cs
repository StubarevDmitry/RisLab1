using Core.Model;

namespace Core.Interfaces
{
    public interface IHashedPasswordStorage
    {
        (string, bool) CreateNew(string hash, int workerCount);
        HashedPassword? GetHashedPassword(string id);
        void SetWorkerCompleted(string id, int partNumber, string[]? passwords, int taskStatus);
        bool AllWorkersCompleted(string id);
        bool PartiallyСompleted(string requestId);
    }
}
