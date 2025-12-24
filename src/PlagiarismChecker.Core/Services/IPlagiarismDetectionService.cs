using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public interface IPlagiarismDetectionService
{
    Task<AnalysisResult> AnalyzeDocumentsAsync(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms,
        CancellationToken cancellationToken = default);

    AnalysisResult AnalyzeDocuments(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms,
        IProgress<double>? progress = null);

    AnalysisResult AnalyzeDocuments(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms);
}