namespace PlagiarismChecker.Core.Services;

public interface IFileTextExtractor
{
    bool CanExtract(string filePath);
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}