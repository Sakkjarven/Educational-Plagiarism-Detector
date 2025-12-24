namespace PlagiarismChecker.Core.Models;

public class ComparisonResult
{
    public Guid DocumentAId { get; init; }
    public Guid DocumentBId { get; init; }
    public string DocumentAName { get; init; }
    public string DocumentBName { get; init; }
    public double SimilarityScore { get; init; }
    public string AlgorithmUsed { get; init; }

    public ComparisonResult(
        Guid documentAId, Guid documentBId,
        string documentAName, string documentBName,
        double similarityScore, string algorithmUsed)
    {
        DocumentAId = documentAId;
        DocumentBId = documentBId;
        DocumentAName = documentAName;
        DocumentBName = documentBName;
        SimilarityScore = Math.Clamp(similarityScore, 0, 1);
        AlgorithmUsed = algorithmUsed;
    }
}