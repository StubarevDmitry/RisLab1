using System.Security.Cryptography;
using Combinatorics.Collections;
using System.Text;

namespace Worker.Services;

public class HashService
{
    private readonly ILogger<HashService> _logger;

    public HashService(ILogger<HashService> logger)
    {
        _logger = logger;
    }

    public List<string> FindMatches(
        string targetHash,
        List<string> alphabet,
        int maxLength,
        int partNumber,
        int partCount,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        var alphabetSize = alphabet.Count;

        var (startCharIndex, endCharIndex) = GetCharRangeForWorker(partNumber, partCount, alphabetSize);

        var totalProcessed = 0L;

        for (int firstCharIdx = startCharIndex; firstCharIdx < endCharIndex; firstCharIdx++)
        {
            var firstChar = alphabet[firstCharIdx];

            for (int length = 1; length <= maxLength; length++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = GenerateCombinationsIterative(
                    firstChar,
                    length - 1,
                    targetHash,
                    alphabet,
                    results,
                    cancellationToken);

                totalProcessed += count;

                if (totalProcessed % 10000 == 0)
                {
                    _logger.LogDebug("комбинаций промотренно {Count}", totalProcessed);
                }
            }
        }

        _logger.LogInformation("нашлось вот столько слов: {MatchCount}",results.Count);

        return results;
    }

    private long GenerateCombinationsIterative(
    string prefix,
    int remainingLength,
    string targetHash,
    List<string> alphabet,
    List<string> results,
    CancellationToken cancellationToken)
    {
        var processed = 0L;

        try
        {
            var permutations = new Variations<string>(alphabet, remainingLength, GenerateOption.WithRepetition);

            foreach (var combination in permutations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var wordBuilder = new StringBuilder(prefix);
                foreach (var letter in combination)
                {
                    wordBuilder.Append(letter);
                }
                var word = wordBuilder.ToString();

                var hash = ComputeMd5Hash(word);
                if (hash.Equals(targetHash, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(word);
                }

                processed++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ошибка при генерации комбинаций: {ex.Message}");
        }

        return processed;
    }

    private (int startIndex, int endIndex) GetCharRangeForWorker(int partNumber, int partCount, int alphabetSize)
    {
        var baseCharsPerWorker = alphabetSize / partCount;
        var remainder = alphabetSize % partCount;

        var startIndex = partNumber * baseCharsPerWorker + Math.Min(partNumber, remainder);
        var endIndex = startIndex + baseCharsPerWorker + (partNumber < remainder ? 1 : 0);

        return (startIndex, endIndex);
    }

    private string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}