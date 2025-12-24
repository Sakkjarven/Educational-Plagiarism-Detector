using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlagiarismChecker.Core;
using PlagiarismChecker.Core.Models;
using PlagiarismChecker.Core.Services;
using System.Text.Json;

namespace PlagiarismChecker.Cli;

class Program
{
    private static IServiceProvider? _serviceProvider;
    private static ILogger<Program>? _logger;

    static async Task Main(string[] args)
    {
        try
        {
            // Настройка DI
            ConfigureServices();

            // Получаем логгер
            _logger = _serviceProvider!.GetRequiredService<ILogger<Program>>();

            _logger.LogInformation("=== Educational Plagiarism Detector ===");
            _logger.LogInformation("Starting analysis...");

            // Парсим аргументы командной строки
            var options = ParseCommandLineArgs(args);

            if (options.ShowHelp)
            {
                ShowHelp();
                return;
            }

            // Загружаем документы
            var fileService = _serviceProvider.GetRequiredService<IFileService>();
            var documents = await fileService.LoadDocumentsFromDirectoryAsync(options.InputDirectory);

            var documentList = documents.ToList();
            if (!documentList.Any())
            {
                _logger.LogError("No documents found in directory: {Directory}", options.InputDirectory);
                Console.WriteLine($"Error: No documents found in '{options.InputDirectory}'");
                return;
            }

            _logger.LogInformation("Loaded {DocumentCount} documents", documentList.Count);

            // Анализируем документы
            var detectionService = _serviceProvider.GetRequiredService<IPlagiarismDetectionService>();
            var algorithms = GetAlgorithms(options.AlgorithmTypes, _logger);

            var result = detectionService.AnalyzeDocuments(documentList, algorithms);

            // Выводим результаты
            DisplayResults(result, options);

            // Сохраняем результаты в JSON если нужно
            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                await SaveResultsToJsonAsync(result, options.OutputFile, _logger);
            }

            _logger.LogInformation("Analysis completed successfully!");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred during analysis");
            Console.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
        finally
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Добавляем логирование
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Добавляем основные сервисы
        services.AddPlagiarismCheckerCore();

        _serviceProvider = services.BuildServiceProvider();
    }

    private static CommandLineOptions ParseCommandLineArgs(string[] args)
    {
        var options = new CommandLineOptions
        {
            InputDirectory = "sample-data", // По умолчанию
            AlgorithmTypes = new List<string> { "all" },
            OutputFile = $"results_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            ShowMatrix = true,
            Threshold = 0.3
        };

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length)
                        options.InputDirectory = args[++i];
                    break;

                case "-a":
                case "--algorithms":
                    if (i + 1 < args.Length)
                        options.AlgorithmTypes = args[++i].Split(',').Select(s => s.Trim()).ToList();
                    break;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        options.OutputFile = args[++i];
                    break;

                case "-t":
                case "--threshold":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out var threshold))
                        options.Threshold = Math.Clamp(threshold, 0, 1);
                    break;

                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;

                case "--no-matrix":
                    options.ShowMatrix = false;
                    break;
            }
        }

        return options;
    }

    private static IEnumerable<SimilarityAlgorithmType> GetAlgorithms(
        List<string> algorithmTypes,
        ILogger<Program>? logger = null)
    {
        if (algorithmTypes.Contains("all", StringComparer.OrdinalIgnoreCase))
        {
            return Enum.GetValues<SimilarityAlgorithmType>();
        }

        var algorithms = new List<SimilarityAlgorithmType>();

        foreach (var type in algorithmTypes)
        {
            if (Enum.TryParse<SimilarityAlgorithmType>(type, true, out var algorithm))
            {
                algorithms.Add(algorithm);
            }
            else
            {
                logger?.LogWarning("Unknown algorithm type: {AlgorithmType}. Skipping.", type);
            }
        }

        return algorithms.Any()
            ? algorithms
            : Enum.GetValues<SimilarityAlgorithmType>();
    }

    private static void DisplayResults(AnalysisResult result, CommandLineOptions options)
    {
        Console.WriteLine("\n=== ANALYSIS RESULTS ===\n");
        Console.WriteLine($"Analysis ID: {result.Id}");
        Console.WriteLine($"Timestamp: {result.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Documents analyzed: {result.Documents.Count}");
        Console.WriteLine($"Comparisons made: {result.ComparisonResults.Count}\n");

        // Группируем результаты по парам документов
        var documentPairs = result.ComparisonResults
            .GroupBy(c => new { c.DocumentAId, c.DocumentBId, c.DocumentAName, c.DocumentBName })
            .Select(g => new
            {
                g.Key.DocumentAName,
                g.Key.DocumentBName,
                Algorithms = g.ToDictionary(c => c.AlgorithmUsed, c => c.SimilarityScore),
                MaxSimilarity = g.Max(c => c.SimilarityScore),
                AvgSimilarity = g.Average(c => c.SimilarityScore)
            })
            .Where(p => p.MaxSimilarity >= options.Threshold)
            .OrderByDescending(p => p.MaxSimilarity)
            .ToList();

        if (documentPairs.Any())
        {
            Console.WriteLine($"=== POTENTIAL PLAGIARISM DETECTED ===\n");
            Console.WriteLine($"Threshold: {options.Threshold:P0}\n");

            foreach (var pair in documentPairs)
            {
                Console.WriteLine($"{pair.DocumentAName} vs {pair.DocumentBName}");
                Console.WriteLine($"  Maximum similarity: {pair.MaxSimilarity:P2}");
                Console.WriteLine($"  Average similarity: {pair.AvgSimilarity:P2}");

                foreach (var algo in pair.Algorithms)
                {
                    Console.WriteLine($"    {algo.Key}: {algo.Value:P2}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"No potential plagiarism detected (threshold: {options.Threshold:P0}).");
        }

        // Выводим матрицу схожести если нужно
        if (options.ShowMatrix && result.Documents.Count <= 10)
        {
            DisplaySimilarityMatrix(result, options.Threshold);
        }
    }

    private static void DisplaySimilarityMatrix(AnalysisResult result, double threshold)
    {
        Console.WriteLine("\n=== SIMILARITY MATRIX (Maximum similarity) ===\n");

        var docs = result.Documents.OrderBy(d => d.Name).ToList();

        // Заголовок
        Console.Write($"{"",-20}");
        foreach (var doc in docs)
        {
            var displayName = doc.Name.Length > 10
                ? doc.Name[..7] + "..."
                : doc.Name;
            Console.Write($"{displayName,-12}");
        }
        Console.WriteLine();

        // Данные
        for (int i = 0; i < docs.Count; i++)
        {
            var docA = docs[i];
            var displayName = docA.Name.Length > 20
                ? docA.Name[..17] + "..."
                : docA.Name;
            Console.Write($"{displayName,-20}");

            for (int j = 0; j < docs.Count; j++)
            {
                if (i == j)
                {
                    Console.Write($"{"--",-12}");
                }
                else
                {
                    var similarity = result.GetSimilarity(docA.Id, docs[j].Id);

                    // Цветовое кодирование
                    if (similarity >= threshold)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (similarity >= threshold / 2)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.Write($"{similarity:P0,-12}");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        Console.ResetColor();
        Console.WriteLine("\nColor key: Green < 50% threshold, Yellow: 50-99%, Red: >= threshold");
    }

    private static async Task SaveResultsToJsonAsync(
        AnalysisResult result,
        string outputFile,
        ILogger<Program>? logger = null)
    {
        try
        {
            // Создаем директорию если нужно
            var directory = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(result, options);
            await File.WriteAllTextAsync(outputFile, json);

            var fullPath = Path.GetFullPath(outputFile);
            logger?.LogInformation("Results saved to: {OutputFile}", fullPath);
            Console.WriteLine($"\nResults saved to: {fullPath}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to save results to JSON file");
            Console.WriteLine($"Warning: Could not save results to {outputFile}: {ex.Message}");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Educational Plagiarism Detector - Command Line Interface");
        Console.WriteLine("Version: 1.0.0");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  plagiarism-checker [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -i, --input <path>     Input directory with documents (default: sample-data)");
        Console.WriteLine("  -a, --algorithms <list> Comma-separated list of algorithms");
        Console.WriteLine("                         Options: CosineSimilarity, LongestCommonSubsequence,");
        Console.WriteLine("                                  NGram, all (default: all)");
        Console.WriteLine("  -o, --output <file>    Output JSON file for results");
        Console.WriteLine("                         (default: results_YYYYMMDD_HHMMSS.json)");
        Console.WriteLine("  -t, --threshold <0-1>  Similarity threshold for plagiarism detection");
        Console.WriteLine("                         (default: 0.3)");
        Console.WriteLine("  --no-matrix            Don't display similarity matrix");
        Console.WriteLine("  -h, --help             Show this help message");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  plagiarism-checker");
        Console.WriteLine("  plagiarism-checker -i ./documents -a CosineSimilarity,NGram");
        Console.WriteLine("  plagiarism-checker --input ./submissions --threshold 0.5");
        Console.WriteLine("  plagiarism-checker --no-matrix");
        Console.WriteLine("  plagiarism-checker --help");
        Console.WriteLine("\nFile formats supported:");
        Console.WriteLine("  • .txt - Plain text files");
        Console.WriteLine("  • .md  - Markdown files");
        Console.WriteLine("  • .pdf - PDF files (extraction not fully implemented)");
    }
}

public class CommandLineOptions
{
    public string InputDirectory { get; set; } = "sample-data";
    public List<string> AlgorithmTypes { get; set; } = new() { "all" };
    public string? OutputFile { get; set; }
    public double Threshold { get; set; } = 0.3;
    public bool ShowHelp { get; set; }
    public bool ShowMatrix { get; set; } = true;
}