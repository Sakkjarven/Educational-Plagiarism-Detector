using FluentAssertions;
using Moq;
using PlagiarismChecker.Core.Services;
using PlagiarismChecker.Core.Services.Algorithms;
using Xunit;

namespace PlagiarismChecker.Tests.Services.Algorithms;

public class LongestCommonSubsequenceAlgorithmTests
{
    private readonly Mock<ITextProcessor> _textProcessorMock;
    private readonly LongestCommonSubsequenceAlgorithm _algorithm;

    public LongestCommonSubsequenceAlgorithmTests()
    {
        _textProcessorMock = new Mock<ITextProcessor>();

        _textProcessorMock.Setup(tp => tp.PreprocessText(It.IsAny<string>()))
            .Returns<string>(text => text.ToLowerInvariant());

        _textProcessorMock.Setup(tp => tp.Tokenize(It.IsAny<string>()))
            .Returns<string>(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        _textProcessorMock.Setup(tp => tp.Lemmatize(It.IsAny<string[]>()))
            .Returns<string[]>(tokens => tokens);

        _algorithm = new LongestCommonSubsequenceAlgorithm(_textProcessorMock.Object);
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
    public void CalculateSimilarity_ShouldReturnCorrectSimilarityForPartialMatch()
    {
        // Arrange
        var textA = "hello world programming test";
        var textB = "world test programming hello"; // Такие же слова, другой порядок

        // Act
        var similarity = _algorithm.CalculateSimilarity(textA, textB);

        // Assert
        similarity.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void CalculateSimilarity_ShouldHandleReorderedText()
    {
        // Arrange
        var textA = "a b c d e f g";
        var textB = "g f e d c b a";

        // Act
        var similarity = _algorithm.CalculateSimilarity(textA, textB);

        // Assert
        similarity.Should().Be(1.0); // Все слова одинаковые, LCS = длина текста
    }
}