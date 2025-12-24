// Пока создаем заглушку для PDF. Для реального проекта нужно добавить библиотеку типа iTextSharp
using PlagiarismChecker.Core.Services;

public class PdfTextExtractor : IFileTextExtractor
{
    public bool CanExtract(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".pdf";
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Временная заглушка - возвращаем сообщение об ошибке
        throw new NotSupportedException(
            "PDF extraction is not implemented. Please convert PDF to text first or install iTextSharp package.");
    }
}
