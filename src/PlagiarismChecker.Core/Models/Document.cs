namespace PlagiarismChecker.Core.Models;

public class Document
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string OriginalText { get; init; }
    public string ProcessedText { get; set; }
    public string[] Tokens { get; set; }

    public Document(string name, string text)
    {
        Id = Guid.NewGuid();
        Name = name;
        OriginalText = text;
        ProcessedText = string.Empty;
        Tokens = Array.Empty<string>();
    }
}