using System.Text.Json.Serialization;

namespace PlagiarismChecker.Core.Models;

public class AnalysisResult
{
    public Guid Id { get; init; }
    public DateTime Timestamp { get; init; }
    public List<Document> Documents { get; init; }
    public List<ComparisonResult> ComparisonResults { get; init; }

    [JsonIgnore]
    public Dictionary<(Guid, Guid), ComparisonResult> ComparisonMatrix { get; private set; }

    public AnalysisResult(List<Document> documents, List<ComparisonResult> comparisonResults)
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        Documents = documents;
        ComparisonResults = comparisonResults;
        BuildMatrix();
    }

    private void BuildMatrix()
    {
        ComparisonMatrix = new Dictionary<(Guid, Guid), ComparisonResult>();
        foreach (var result in ComparisonResults)
        {
            ComparisonMatrix[(result.DocumentAId, result.DocumentBId)] = result;
        }
    }

    public double GetSimilarity(Guid docAId, Guid docBId)
    {
        return ComparisonMatrix.TryGetValue((docAId, docBId), out var result)
            ? result.SimilarityScore
            : 0;
    }
}