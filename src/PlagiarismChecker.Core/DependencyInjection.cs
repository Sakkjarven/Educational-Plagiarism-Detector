using Microsoft.Extensions.DependencyInjection;
using PlagiarismChecker.Core.Services;
using PlagiarismChecker.Core.Services.Algorithms;
using PlagiarismChecker.Core.Services.FileTextExtractors;

namespace PlagiarismChecker.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddPlagiarismCheckerCore(this IServiceCollection services)
    {
        // Регистрируем сервисы
        services.AddSingleton<ITextProcessor, TextProcessor>();
        services.AddSingleton<IAlgorithmFactory, AlgorithmFactory>();

        // Регистрируем алгоритмы
        services.AddTransient<CosineSimilarityAlgorithm>();
        services.AddTransient<LongestCommonSubsequenceAlgorithm>();
        services.AddTransient<NGramSimilarityAlgorithm>();

        // Регистрируем основные сервисы
        services.AddScoped<IPlagiarismDetectionService, PlagiarismDetectionService>();
        services.AddScoped<IFileService, FileService>();

        // Регистрируем экстракторы текста
        services.AddSingleton<IFileTextExtractor, PlainTextExtractor>();
        services.AddSingleton<IFileTextExtractor, PdfTextExtractor>();

        return services;
    }
}