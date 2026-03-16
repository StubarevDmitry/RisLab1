namespace Core.Model
{
    public class HashedPassword
    {
        public string Hash { get; set; }
        public string[]? Passwords { get; set; }
        public int[] WorkerCompleted { get; set; }

        public HashedPassword(string hash, int workerCount)
        {
            Hash = hash;
            WorkerCompleted = new int[workerCount];
            Passwords = null;
        }
    }
}
