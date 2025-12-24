using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Reflection.PortableExecutable;
using System.Text;

namespace PlagiarismChecker.Core.Services.FileTextExtractors;

public class PdfTextExtractor : IFileTextExtractor
{
    public bool CanExtract(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".pdf";
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                using (var reader = new PdfReader(filePath))
                {
                    for (int page = 1; page <= reader.NumberOfPages; page++)
                    {
                        var pageText = PdfTextExtractor.GetTextFromPage(reader, page);
                        text.AppendLine(pageText);
                    }
                }

                return text.ToString();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract text from PDF: {filePath}", ex);
        }
    }
}   