using Microsoft.Extensions.Logging;
using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public class PlagiarismDetectionService : IPlagiarismDetectionService
{
    private readonly ITextProcessor _textProcessor;
    private readonly IAlgorithmFactory _algorithmFactory; // Изменил тип
    private readonly ILogger<PlagiarismDetectionService> _logger;

    public PlagiarismDetectionService(
        ITextProcessor textProcessor,
        IAlgorithmFactory algorithmFactory, // Изменил тип
        ILogger<PlagiarismDetectionService> logger)
    {
        _textProcessor = textProcessor;
        _algorithmFactory = algorithmFactory;
        _logger = logger;
    }

    public AnalysisResult AnalyzeDocuments(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms)
    {
        var documentList = documents.ToList();
        _logger.LogInformation("Starting plagiarism analysis for {DocumentCount} documents", documentList.Count);

        // Предобработка документов
        var processedDocuments = ProcessDocuments(documentList);

        // Сравнение документов
        var comparisonResults = CompareDocuments(processedDocuments, algorithms);

        // Создаем результат анализа
        var result = new AnalysisResult(processedDocuments, comparisonResults);

        _logger.LogInformation("Analysis completed. Generated {ComparisonCount} comparisons",
            comparisonResults.Count);

        return result;
    }

    public Task<AnalysisResult> AnalyzeDocumentsAsync(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AnalyzeDocuments(documents, algorithms));
    }

    private List<Document> ProcessDocuments(List<Document> documents)
    {
        var processed = new List<Document>();

        foreach (var document in documents)
        {
            _logger.LogDebug("Processing document: {DocumentName}", document.Name);

            var processedText = _textProcessor.PreprocessText(document.OriginalText);
            var tokens = _textProcessor.Tokenize(processedText);
            var lemmas = _textProcessor.Lemmatize(tokens);

            var processedDocument = new Document(document.Name, document.OriginalText)
            {
                ProcessedText = processedText,
                Tokens = lemmas
            };

            processed.Add(processedDocument);

            _logger.LogDebug("Document processed. Original length: {Original}, Tokens: {TokenCount}",
                document.OriginalText.Length, lemmas.Length);
        }

        return processed;
    }

    private List<ComparisonResult> CompareDocuments(
        List<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithmTypes)
    {
        var comparisonResults = new List<ComparisonResult>();
        var algorithms = algorithmTypes
            .Select(t => _algorithmFactory.CreateAlgorithm(t))
            .ToList();

        // Сравниваем каждую пару документов
        for (int i = 0; i < documents.Count; i++)
        {
            for (int j = i + 1; j < documents.Count; j++)
            {
                var docA = documents[i];
                var docB = documents[j];

                _logger.LogDebug("Comparing: {DocA} vs {DocB}", docA.Name, docB.Name);

                foreach (var algorithm in algorithms)
                {
                    try
                    {
                        var similarity = algorithm.CalculateSimilarity(
                            docA.OriginalText,
                            docB.OriginalText);

                        var result = new ComparisonResult(
                            docA.Id, docB.Id,
                            docA.Name, docB.Name,
                            similarity,
                            algorithm.AlgorithmType.ToString());

                        comparisonResults.Add(result);

                        _logger.LogDebug("Algorithm {Algorithm}: Similarity = {Similarity:P2}",
                            algorithm.AlgorithmType, similarity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error comparing documents with algorithm {Algorithm}",
                            algorithm.AlgorithmType);
                    }
                }
            }
        }

        return comparisonResults;
    }
}