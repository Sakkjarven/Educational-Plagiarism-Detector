using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services.Algorithms;

public class LongestCommonSubsequenceAlgorithm : ISimilarityAlgorithm
{
    private readonly ITextProcessor _textProcessor;

    public SimilarityAlgorithmType AlgorithmType => SimilarityAlgorithmType.LongestCommonSubsequence;

    public LongestCommonSubsequenceAlgorithm(ITextProcessor textProcessor)
    {
        _textProcessor = textProcessor;
    }

    public double CalculateSimilarity(string textA, string textB)
    {
        if (string.IsNullOrWhiteSpace(textA) || string.IsNullOrWhiteSpace(textB))
            return 0.0;

        // Предобработка текста
        var processedA = _textProcessor.PreprocessText(textA);
        var processedB = _textProcessor.PreprocessText(textB);

        // Токенизация
        var tokensA = _textProcessor.Tokenize(processedA);
        var tokensB = _textProcessor.Tokenize(processedB);

        // Лемматизация
        var lemmasA = _textProcessor.Lemmatize(tokensA);
        var lemmasB = _textProcessor.Lemmatize(tokensB);

        // Если оба документа пустые после обработки
        if (lemmasA.Length == 0 && lemmasB.Length == 0)
            return 1.0;
        if (lemmasA.Length == 0 || lemmasB.Length == 0)
            return 0.0;

        // Небольшое улучшение: если наборы лемм совпадают (игнорируем порядок), считаем полным соответствием
        if (lemmasA.Length == lemmasB.Length && lemmasA.OrderBy(x => x).SequenceEqual(lemmasB.OrderBy(x => x)))
            return 1.0;

        // Вычисляем LCS
        var lcsLength = CalculateLCSLength(lemmasA, lemmasB);

        // Нормализуем результат (длина LCS / средняя длина текстов)
        var avgLength = (lemmasA.Length + lemmasB.Length) / 2.0;
        var similarity = lcsLength / avgLength;

        return Math.Clamp(similarity, 0, 1);
    }

    private int CalculateLCSLength(string[] sequenceA, string[] sequenceB)
    {
        int m = sequenceA.Length;
        int n = sequenceB.Length;

        var dp = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
        {
            for (int j = 0; j <= n; j++)
            {
                if (i == 0 || j == 0)
                    dp[i, j] = 0;
                else if (sequenceA[i - 1] == sequenceB[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return dp[m, n];
    }
}