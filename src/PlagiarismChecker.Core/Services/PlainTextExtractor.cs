namespace PlagiarismChecker.Core.Services.FileTextExtractors;

public class PlainTextExtractor : IFileTextExtractor
{
    public bool CanExtract(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".txt" || extension == ".md";
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read text file: {filePath}", ex);
        }
    }
}