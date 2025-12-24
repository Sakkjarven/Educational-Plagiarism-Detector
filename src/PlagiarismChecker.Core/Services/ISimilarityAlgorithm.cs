using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public interface ISimilarityAlgorithm
{
    SimilarityAlgorithmType AlgorithmType { get; }
    double CalculateSimilarity(string textA, string textB);
}