using Microsoft.Extensions.Logging;
using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public class FileService : IFileService
{
    private readonly IEnumerable<IFileTextExtractor> _extractors;
    private readonly ILogger<FileService> _logger;

    public FileService(IEnumerable<IFileTextExtractor> extractors, ILogger<FileService> logger)
    {
        _extractors = extractors;
        _logger = logger;
    }

    public async Task<IEnumerable<Document>> LoadDocumentsFromDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var documents = new List<Document>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return documents;
        }

        var files = Directory.GetFiles(directoryPath);
        _logger.LogInformation("Found {FileCount} files in directory: {DirectoryPath}",
            files.Length, directoryPath);

        foreach (var filePath in files)
        {
            try
            {
                var document = await LoadDocumentAsync(filePath, cancellationToken);
                if (document != null)
                {
                    documents.Add(document);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load document: {FilePath}", filePath);
            }
        }

        return documents;
    }

    public async Task<Document?> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(filePath));

        if (extractor == null)
        {
            _logger.LogWarning("No extractor found for file: {FilePath}", filePath);
            return null;
        }

        try
        {
            var fileName = Path.GetFileName(filePath);
            var text = await extractor.ExtractTextAsync(filePath, cancellationToken);

            _logger.LogDebug("Loaded document: {FileName}, Size: {Size} characters",
                fileName, text.Length);

            return new Document(fileName, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from file: {FilePath}", filePath);
            return null;
        }
    }
}
