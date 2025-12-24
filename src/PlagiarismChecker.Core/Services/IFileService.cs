using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public interface IFileService
{
    Task<IEnumerable<Document>> LoadDocumentsFromDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);

    Task<Document?> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default);
}