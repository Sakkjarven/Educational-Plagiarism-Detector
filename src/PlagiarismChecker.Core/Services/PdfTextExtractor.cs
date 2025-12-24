namespace PlagiarismChecker.Core.Services.FileTextExtractors;

public class PdfTextExtractor : IFileTextExtractor
{
    public bool CanExtract(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".pdf";
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Возвращаем пустой текст для PDF
        return Task.FromResult("[PDF content - extraction requires additional setup]");
    }
}