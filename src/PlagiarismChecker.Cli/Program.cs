using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlagiarismChecker.Core;
using PlagiarismChecker.Core.Models;
using PlagiarismChecker.Core.Services;
using Spectre.Console;
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

            AnalysisResult result = null!; // Инициализация переменной

            if (options.ShowProgress)
            {
                // Создаем прогресс-бар и запускаем анализ с прогрессом
                AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn()
                    })
                    .Start(ctx =>
                    {
                        var progressTask = ctx.AddTask("[green]Analyzing documents...[/]");
                        progressTask.IsIndeterminate = false;

                        var progress = new Progress<double>(value =>
                        {
                            progressTask.Value = value * 100;
                        });

                        result = detectionService.AnalyzeDocuments(documentList, algorithms, progress);
                        progressTask.Value = 100;
                    });
            }
            else
            {
                // Анализ без прогресса
                result = detectionService.AnalyzeDocuments(documentList, algorithms);
            }

            // Отобразить результаты в консоли
            DisplayResults(result, options);

            // Сохранить результаты в JSON (если указан путь)
            if (!string.IsNullOrWhiteSpace(options.OutputFile))
            {
                await SaveResultsToJsonAsync(result, options.OutputFile!, _logger);
            }
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
                case "--format":
                    if (i + 1 < args.Length)
                        options.ExportFormat = args[++i];
                    break;

                case "--no-progress":
                    options.ShowProgress = false;
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
        AnsiConsole.WriteLine();

        // Заголовок
        var rule = new Rule("[bold blue]ANALYSIS RESULTS[/]");
        rule.Style = Style.Parse("blue dim");
        AnsiConsole.Write(rule);

        // Основная информация
        var infoTable = new Table();
        infoTable.Border(TableBorder.None);
        infoTable.AddColumn("");
        infoTable.AddColumn("");

        infoTable.AddRow("[bold]Analysis ID:[/]", result.Id.ToString());
        infoTable.AddRow("[bold]Timestamp:[/]", result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        infoTable.AddRow("[bold]Documents analyzed:[/]", result.Documents.Count.ToString());
        infoTable.AddRow("[bold]Comparisons made:[/]", result.ComparisonResults.Count.ToString());
        infoTable.AddRow("[bold]Similarity threshold:[/]", $"{options.Threshold:P0}");

        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();

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
            var rule2 = new Rule("[bold red]⚠️ POTENTIAL PLAGIARISM DETECTED[/]");
            rule2.Style = Style.Parse("red");
            AnsiConsole.Write(rule2);

            // Создаем таблицу с результатами
            var resultsTable = new Table();
            resultsTable.Border(TableBorder.Rounded);
            resultsTable.AddColumn("[bold]Document A[/]");
            resultsTable.AddColumn("[bold]Document B[/]");
            resultsTable.AddColumn(new TableColumn("[bold]Max Similarity[/]").Centered());
            resultsTable.AddColumn(new TableColumn("[bold]Avg Similarity[/]").Centered());
            resultsTable.AddColumn("[bold]Algorithms Used[/]");

            foreach (var pair in documentPairs)
            {
                var maxSimilarityFormatted = pair.MaxSimilarity >= options.Threshold * 1.5
                    ? $"[red on white bold]{pair.MaxSimilarity:P0}[/]"
                    : pair.MaxSimilarity >= options.Threshold
                        ? $"[red]{pair.MaxSimilarity:P0}[/]"
                        : $"[orange3]{pair.MaxSimilarity:P0}[/]";

                var avgSimilarityFormatted = pair.AvgSimilarity >= options.Threshold
                    ? $"[red]{pair.AvgSimilarity:P0}[/]"
                    : $"[orange3]{pair.AvgSimilarity:P0}[/]";

                var algorithms = string.Join(", ", pair.Algorithms
                    .Select(a => $"{a.Key}: {a.Value:P0}"));

                resultsTable.AddRow(
                    pair.DocumentAName,
                    pair.DocumentBName,
                    maxSimilarityFormatted,
                    avgSimilarityFormatted,
                    algorithms);
            }

            AnsiConsole.Write(resultsTable);
            AnsiConsole.WriteLine();
        }
        else
        {
            var panel = new Panel("[green]✅ No potential plagiarism detected[/]")
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1)
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        // Выводим матрицу схожести если нужно
        if (options.ShowMatrix && result.Documents.Count <= 15)
        {
            DisplaySimilarityMatrix(result, options.Threshold);
        }
    }

    private static void DisplaySimilarityMatrix(AnalysisResult result, double threshold)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel("[bold blue]SIMILARITY MATRIX (Maximum similarity)[/]")
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        };
        AnsiConsole.Write(panel);

        var docs = result.Documents.OrderBy(d => d.Name).ToList();

        // Создаем таблицу
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Document[/]").LeftAligned());

        // Добавляем заголовки
        foreach (var doc in docs)
        {
            var displayName = doc.Name.Length > 15
                ? doc.Name[..12] + "..."
                : doc.Name;
            table.AddColumn(new TableColumn($"[bold]{displayName}[/]").Centered());
        }

        // Добавляем данные
        for (int i = 0; i < docs.Count; i++)
        {
            var docA = docs[i];
            var displayName = docA.Name.Length > 20
                ? docA.Name[..17] + "..."
                : docA.Name;

            var rowData = new List<string> { $"[bold]{displayName}[/]" };

            for (int j = 0; j < docs.Count; j++)
            {
                if (i == j)
                {
                    rowData.Add("[grey]--[/]");
                }
                else
                {
                    var similarity = result.GetSimilarity(docA.Id, docs[j].Id);
                    var similarityPercent = similarity.ToString("P0");

                    // Цветовое кодирование
                    string styledText;
                    if (similarity >= threshold)
                        styledText = $"[red on white bold]{similarityPercent}[/]";
                    else if (similarity >= threshold * 0.5)
                        styledText = $"[orange3]{similarityPercent}[/]";
                    else if (similarity >= threshold * 0.3)
                        styledText = $"[yellow]{similarityPercent}[/]";
                    else
                        styledText = $"[green]{similarityPercent}[/]";

                    rowData.Add(styledText);
                }
            }

            table.AddRow(rowData.ToArray());
        }

        AnsiConsole.Write(table);

        // Легенда
        var legend = new Table();
        legend.Border(TableBorder.None);
        legend.AddColumn("Color Legend");
        legend.AddRow("[green]Green:[/] Low similarity (< 30% threshold)");
        legend.AddRow("[yellow]Yellow:[/] Moderate similarity (30-50% threshold)");
        legend.AddRow("[orange3]Orange:[/] High similarity (50-99% threshold)");
        legend.AddRow("[red on white]Red/White:[/] Potential plagiarism (≥ threshold)");

        AnsiConsole.Write(legend);
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
    public string? ExportFormat { get; set; } = "json"; // json или csv
    public double Threshold { get; set; } = 0.3;
    public bool ShowHelp { get; set; }
    public bool ShowMatrix { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
}