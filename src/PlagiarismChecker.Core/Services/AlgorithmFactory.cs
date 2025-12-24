using PlagiarismChecker.Core.Models;
using PlagiarismChecker.Core.Services.Algorithms;

namespace PlagiarismChecker.Core.Services;

public class AlgorithmFactory
{
    private readonly ITextProcessor _textProcessor;

    public AlgorithmFactory(ITextProcessor textProcessor)
    {
        _textProcessor = textProcessor;
    }

    public ISimilarityAlgorithm CreateAlgorithm(SimilarityAlgorithmType algorithmType)
    {
        return algorithmType switch
        {
            SimilarityAlgorithmType.CosineSimilarity =>
                new CosineSimilarityAlgorithm(_textProcessor),

            SimilarityAlgorithmType.LongestCommonSubsequence =>
                new LongestCommonSubsequenceAlgorithm(_textProcessor),

            SimilarityAlgorithmType.NGram =>
                new NGramSimilarityAlgorithm(_textProcessor),

            _ => throw new ArgumentException($"Unknown algorithm type: {algorithmType}")
        };
    }

    public IEnumerable<ISimilarityAlgorithm> CreateAllAlgorithms()
    {
        yield return CreateAlgorithm(SimilarityAlgorithmType.CosineSimilarity);
        yield return CreateAlgorithm(SimilarityAlgorithmType.LongestCommonSubsequence);
        yield return CreateAlgorithm(SimilarityAlgorithmType.NGram);
    }
}