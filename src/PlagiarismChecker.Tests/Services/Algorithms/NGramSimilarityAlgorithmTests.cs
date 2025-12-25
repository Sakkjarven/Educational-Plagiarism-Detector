using FluentAssertions;
using Moq;
using PlagiarismChecker.Core.Services;
using PlagiarismChecker.Core.Services.Algorithms;
using Xunit;
namespace PlagiarismChecker.Tests.Services.Algorithms;

public class NGramSimilarityAlgorithmTests
{
    private readonly Mock<ITextProcessor> _textProcessorMock;
    private readonly NGramSimilarityAlgorithm _algorithm;

    public NGramSimilarityAlgorithmTests()
    {
        _textProcessorMock = new Mock<ITextProcessor>();

        _textProcessorMock.Setup(tp => tp.PreprocessText(It.IsAny<string>()))
            .Returns<string>(text => text.ToLowerInvariant());

        _textProcessorMock.Setup(tp => tp.Tokenize(It.IsAny<string>()))
            .Returns<string>(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        _textProcessorMock.Setup(tp => tp.Lemmatize(It.IsAny<string[]>()))
            .Returns<string[]>(tokens => tokens);

        _algorithm = new NGramSimilarityAlgorithm(_textProcessorMock.Object, nGramSize: 2);
    }

    [Fact]
    public void CalculateSimilarity_ShouldReturnOneForIdenticalTexts()
    {
        // Arrange
        var text = "hello world test document";

        // Act
        var similarity = _algorithm.CalculateSimilarity(text, text);

        // Assert
        similarity.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_ShouldDetectNGramSimilarity()
    {
        // Arrange
        var textA = "the quick brown fox jumps";
        var textB = "quick brown fox jumps over";

        // Act
        var similarity = _algorithm.CalculateSimilarity(textA, textB);

        // Assert
        // Должна быть высокая схожесть из-за общих 2-грамм
        similarity.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void CalculateSimilarity_ShouldWorkWithSmallTexts()
    {
        // Arrange
        var textA = "short text";
        var textB = "another short";

        // Act
        var similarity = _algorithm.CalculateSimilarity(textA, textB);

        // Assert
        similarity.Should().BeInRange(0, 1);
    }
}