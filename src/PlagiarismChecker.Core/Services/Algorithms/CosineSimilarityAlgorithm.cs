using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services.Algorithms;

public class CosineSimilarityAlgorithm : ISimilarityAlgorithm
{
    private readonly ITextProcessor _textProcessor;
    private TfIdfVectorizer? _vectorizer;

    public SimilarityAlgorithmType AlgorithmType => SimilarityAlgorithmType.CosineSimilarity;

    public CosineSimilarityAlgorithm(ITextProcessor textProcessor)
    {
        _textProcessor = textProcessor;
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

        // Лемматизация (удаление стоп-слов)
        var lemmasA = _textProcessor.Lemmatize(tokensA);
        var lemmasB = _textProcessor.Lemmatize(tokensB);

        if (lemmasA.Length == 0 && lemmasB.Length == 0)
            return 0.0;

        if (lemmasA.Length == 0 || lemmasB.Length == 0)
            return 0.0;

        // Создаем TF-IDF векторизатор
        var vectorizer = new TfIdfVectorizer();
        var documents = new[] { lemmasA, lemmasB };
        vectorizer.Fit(documents);

        // Преобразуем в векторы
        var vectorA = vectorizer.Transform(lemmasA);
        var vectorB = vectorizer.Transform(lemmasB);

        // Вычисляем косинусное сходство
        var similarity = TfIdfVectorizer.CosineSimilarity(vectorA, vectorB);

        // Ограничиваем результат [0, 1]
        return Math.Clamp(similarity, 0, 1);
    }
}
