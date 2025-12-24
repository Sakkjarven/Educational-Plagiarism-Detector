using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlagiarismChecker.Core.Models;
using PlagiarismChecker.Core.Services;

namespace PlagiarismChecker.Tests.Services;

public class PlagiarismDetectionServiceTests
{
    [Fact]
    public void AnalyzeDocuments_ShouldHandleEmptyDocumentList()
    {
        // Arrange
        var textProcessorMock = new Mock<ITextProcessor>();
        var algorithmFactoryMock = new Mock<IAlgorithmFactory>();
        var loggerMock = new Mock<ILogger<PlagiarismDetectionService>>();

        var service = new PlagiarismDetectionService(
            textProcessorMock.Object,
            algorithmFactoryMock.Object,
            loggerMock.Object);

        var documents = new List<Document>();
        var algorithms = new[] { SimilarityAlgorithmType.CosineSimilarity };

        // Act
        var result = service.AnalyzeDocuments(documents, algorithms);

        // Assert
        result.Should().NotBeNull();
        result.Documents.Should().BeEmpty();
        result.ComparisonResults.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeDocuments_ShouldHandleSingleDocument()
    {
        // Arrange
        var textProcessorMock = new Mock<ITextProcessor>();
        var algorithmFactoryMock = new Mock<IAlgorithmFactory>();
        var loggerMock = new Mock<ILogger<PlagiarismDetectionService>>();

        var service = new PlagiarismDetectionService(
            textProcessorMock.Object,
            algorithmFactoryMock.Object,
            loggerMock.Object);

        var documents = new List<Document>
        {
            new("single.txt", "Test document")
        };

        var algorithms = new[] { SimilarityAlgorithmType.CosineSimilarity };

        // Act
        var result = service.AnalyzeDocuments(documents, algorithms);

        // Assert
        result.Should().NotBeNull();
        result.Documents.Should().HaveCount(1);
        result.ComparisonResults.Should().BeEmpty();
    }
}