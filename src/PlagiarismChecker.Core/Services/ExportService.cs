using PlagiarismChecker.Core.Models;
using System.Text.Json;
namespace PlagiarismChecker.Core.Services;

public interface IExportService
{
    Task ExportToCsvAsync(AnalysisResult result, string filePath, double threshold);
    Task ExportToJsonAsync(AnalysisResult result, string filePath);
}

public class ExportService : IExportService
{
    public async Task ExportToCsvAsync(AnalysisResult result, string filePath, double threshold)
    {
        var lines = new List<string>
        {
            "Document A,Document B,Max Similarity,Average Similarity,Algorithms,Status"
        };

        var pairs = result.ComparisonResults
            .GroupBy(c => new { c.DocumentAId, c.DocumentBId, c.DocumentAName, c.DocumentBName })
            .Select(g => new
            {
                g.Key.DocumentAName,
                g.Key.DocumentBName,
                Algorithms = string.Join(";", g.Select(c => $"{c.AlgorithmUsed}:{c.SimilarityScore:P2}")),
                MaxSimilarity = g.Max(c => c.SimilarityScore),
                AvgSimilarity = g.Average(c => c.SimilarityScore)
            })
            .OrderByDescending(p => p.MaxSimilarity);

        foreach (var pair in pairs)
        {
            var status = pair.MaxSimilarity >= threshold ? "POTENTIAL_PLAGIARISM" : "OK";
            var line = $"\"{pair.DocumentAName}\",\"{pair.DocumentBName}\"," +
                      $"{pair.MaxSimilarity:P2},{pair.AvgSimilarity:P2}," +
                      $"\"{pair.Algorithms}\",{status}";

            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

    public async Task ExportToJsonAsync(AnalysisResult result, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(result, options);
        await File.WriteAllTextAsync(filePath, json);
    }
}