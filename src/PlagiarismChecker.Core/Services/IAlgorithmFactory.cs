using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public interface IAlgorithmFactory
{
    ISimilarityAlgorithm CreateAlgorithm(SimilarityAlgorithmType algorithmType);
    IEnumerable<ISimilarityAlgorithm> CreateAllAlgorithms();
}