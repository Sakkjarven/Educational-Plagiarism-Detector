using System.Collections.Immutable;
using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services.Algorithms;

public class NGramSimilarityAlgorithm : ISimilarityAlgorithm
{
    private readonly ITextProcessor _textProcessor;
    private readonly int _nGramSize;

    public SimilarityAlgorithmType AlgorithmType => SimilarityAlgorithmType.NGram;

    public NGramSimilarityAlgorithm(ITextProcessor textProcessor, int nGramSize = 3)
    {
        _textProcessor = textProcessor;
        _nGramSize = nGramSize > 0 ? nGramSize : 3;
    }

    public double CalculateSimilarity(string textA, string textB)
    {
        if (string.IsNullOrWhiteSpace(textA) || string.IsNullOrWhiteSpace(textB))
            return 0.0;

        // Предобработка текста
        var processedA = _textProcessor.PreprocessText(textA);
        var processedB = _textProcessor.PreprocessText(textB);

        // Токенизация
        var tokensA = _textProcessor.Tokenize(processedA);
        var tokensB = _textProcessor.Tokenize(processedB);

        // Лемматизация
        var lemmasA = _textProcessor.Lemmatize(tokensA);
        var lemmasB = _textProcessor.Lemmatize(tokensB);

        // Если оба документа пустые после обработки
        if (lemmasA.Length == 0 && lemmasB.Length == 0)
            return 1.0;
        if (lemmasA.Length == 0 || lemmasB.Length == 0)
            return 0.0;

        // Генерируем N-граммы
        var nGramsA = GenerateNGrams(lemmasA);
        var nGramsB = GenerateNGrams(lemmasB);

        // Вычисляем коэффициент Жаккара
        var similarity = CalculateJaccardSimilarity(nGramsA, nGramsB);

        return Math.Clamp(similarity, 0, 1);
    }

    private HashSet<string> GenerateNGrams(string[] tokens)
    {
        var nGrams = new HashSet<string>();

        if (tokens.Length < _nGramSize)
        {
            // Если текст короче N, используем весь текст как одну N-грамму
            var singleGram = string.Join(" ", tokens);
            nGrams.Add(singleGram);
            return nGrams;
        }

        for (int i = 0; i <= tokens.Length - _nGramSize; i++)
        {
            var nGram = string.Join(" ", tokens.Skip(i).Take(_nGramSize));
            nGrams.Add(nGram);
        }

        return nGrams;
    }

    private double CalculateJaccardSimilarity(HashSet<string> setA, HashSet<string> setB)
    {
        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();

        if (union == 0)
            return 0.0;

        return (double)intersection / union;
    }
}