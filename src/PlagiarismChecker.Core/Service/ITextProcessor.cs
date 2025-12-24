namespace PlagiarismChecker.Core.Services;

public interface ITextProcessor
{
    string PreprocessText(string text);
    string[] Tokenize(string text);
    string[] Lemmatize(string[] tokens);
}