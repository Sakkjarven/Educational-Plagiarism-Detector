using Microsoft.Extensions.Logging;
using PlagiarismChecker.Core.Models;

namespace PlagiarismChecker.Core.Services;

public class PlagiarismDetectionService : IPlagiarismDetectionService
{
    private readonly ITextProcessor _textProcessor;
    private readonly IAlgorithmFactory _algorithmFactory;
    private readonly ILogger<PlagiarismDetectionService> _logger;

    public PlagiarismDetectionService(
        ITextProcessor textProcessor,
        IAlgorithmFactory algorithmFactory,
        ILogger<PlagiarismDetectionService> logger)
    {
        _textProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));
        _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AnalysisResult AnalyzeDocuments(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        if (algorithms == null) throw new ArgumentNullException(nameof(algorithms));

        var documentList = documents.ToList();
        _logger.LogInformation("Starting plagiarism analysis for {DocumentCount} documents", documentList.Count);

        // Предобработка документов
        var processedDocuments = ProcessDocuments(documentList);

        // Сравнение документов (без прогресса)
        var comparisonResults = CompareDocuments(processedDocuments, algorithms);

        // Создаем результат анализа
        var result = new AnalysisResult(processedDocuments, comparisonResults);

        _logger.LogInformation("Analysis completed. Generated {ComparisonCount} comparisons",
            comparisonResults.Count);

        return result;
    }

    public async Task<AnalysisResult> AnalyzeDocumentsAsync(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms,
        CancellationToken cancellationToken = default)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        if (algorithms == null) throw new ArgumentNullException(nameof(algorithms));

        var documentList = documents.ToList();
        _logger.LogInformation("Starting plagiarism analysis (async) for {DocumentCount} documents", documentList.Count);

        // Run CPU-bound preprocessing and comparisons off the thread pool and support cancellation
        var processedDocuments = await Task.Run(() => ProcessDocuments(documentList), cancellationToken);

        var comparisonResults = await Task.Run(() => CompareDocuments(processedDocuments, algorithms), cancellationToken);

        var result = new AnalysisResult(processedDocuments, comparisonResults);

        _logger.LogInformation("Async analysis completed. Generated {ComparisonCount} comparisons",
            comparisonResults.Count);

        return result;
    }

    private List<Document> ProcessDocuments(List<Document> documents)
    {
        var processed = new List<Document>(documents.Count);

        foreach (var document in documents)
        {
            _logger.LogDebug("Processing document: {DocumentName}", document.Name);

            var processedText = _textProcessor.PreprocessText(document.OriginalText ?? string.Empty);
            var tokens = _textProcessor.Tokenize(processedText) ?? Array.Empty<string>();
            var lemmas = _textProcessor.Lemmatize(tokens) ?? Array.Empty<string>();

            var processedDocument = new Document(document.Name, document.OriginalText)
            {
                ProcessedText = processedText,
                Tokens = lemmas
            };

            processed.Add(processedDocument);

            _logger.LogDebug("Document processed. Original length: {Original}, Tokens: {TokenCount}",
                document.OriginalText?.Length ?? 0, lemmas.Length);
        }

        return processed;
    }

    private List<ComparisonResult> CompareDocuments(
        List<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithmTypes,
        IProgress<double>? progress = null)
    {
        var comparisonResults = new List<ComparisonResult>();

        var algorithmTypeList = algorithmTypes?.ToList() ?? new List<SimilarityAlgorithmType>();
        var algorithms = algorithmTypeList
            .Select(t => _algorithmFactory.CreateAlgorithm(t))
            .ToList();

        int docCount = documents?.Count ?? 0;
        int algoCount = algorithms.Count;
        int totalComparisons = (docCount > 1 && algoCount > 0)
            ? (docCount * (docCount - 1) / 2) * algoCount
            : 0;

        int completedComparisons = 0;

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
                            docA.OriginalText ?? string.Empty,
                            docB.OriginalText ?? string.Empty);

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

                    completedComparisons++;

                    // Report progress only when totalComparisons > 0
                    if (progress != null && totalComparisons > 0)
                    {
                        progress.Report((double)completedComparisons / totalComparisons);
                    }
                }
            }
        }

        // If there were no comparisons but progress requested, report completion
        if (progress != null && totalComparisons == 0)
        {
            progress.Report(1.0);
        }

        return comparisonResults;
    }

    public AnalysisResult AnalyzeDocuments(
        IEnumerable<Document> documents,
        IEnumerable<SimilarityAlgorithmType> algorithms,
        IProgress<double>? progress = null)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        if (algorithms == null) throw new ArgumentNullException(nameof(algorithms));

        var documentList = documents.ToList();
        var algorithmList = algorithms.ToList();

        _logger.LogInformation("Starting plagiarism analysis for {DocumentCount} documents", documentList.Count);

        // Предобработка документов
        var processedDocuments = ProcessDocuments(documentList);

        // Сравнение документов с прогрессом
        var comparisonResults = CompareDocuments(
            processedDocuments,
            algorithmList,
            progress);

        // Создаем результат анализа
        var result = new AnalysisResult(processedDocuments, comparisonResults);

        _logger.LogInformation("Analysis completed. Generated {ComparisonCount} comparisons",
            comparisonResults.Count);

        return result;
    }
}