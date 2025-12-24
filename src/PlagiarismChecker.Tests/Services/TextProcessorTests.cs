using FluentAssertions;
using PlagiarismChecker.Core.Services;

namespace PlagiarismChecker.Tests.Services;

public class TextProcessorTests
{
    private readonly TextProcessor _processor;

    public TextProcessorTests()
    {
        _processor = new TextProcessor();
    }

    [Fact]
    public void PreprocessText_ShouldRemoveHtmlTags()
    {
        // Arrange
        var text = "<p>Hello world</p> and <b>another text</b>";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().Be("hello world and another text");
    }

    [Fact]
    public void PreprocessText_ShouldRemoveUrls()
    {
        // Arrange
        var text = "Visit https://example.com and http://test.ru for more info";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().Be("visit and for more info");
    }

    [Fact]
    public void PreprocessText_ShouldConvertToLowerCase()
    {
        // Arrange
        var text = "Hello World TEST";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().Be("hello world test");
    }

    [Fact]
    public void PreprocessText_ShouldRemoveSpecialCharacters()
    {
        // Arrange
        var text = "Hello, world! Test; test? And...";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().Be("hello world test test and");
    }

    [Fact]
    public void Tokenize_ShouldSplitTextIntoTokens()
    {
        // Arrange
        var text = "hello world this is a test";

        // Act
        var tokens = _processor.Tokenize(text);

        // Assert
        tokens.Should().HaveCount(6);
        tokens.Should().ContainInOrder("hello", "world", "this", "is", "a", "test");
    }

    [Fact]
    public void Tokenize_ShouldFilterOutShortTokens()
    {
        // Arrange
        var text = "a b c hello world";

        // Act
        var tokens = _processor.Tokenize(text);

        // Assert
        tokens.Should().HaveCount(2);
        tokens.Should().ContainInOrder("hello", "world");
    }

    [Fact]
    public void Lemmatize_ShouldRemoveStopWords()
    {
        // Arrange
        var tokens = new[] { "this", "is", "a", "test", "document" };

        // Act
        var lemmas = _processor.Lemmatize(tokens);

        // Assert
        // Слова "this", "is", "a" должны быть удалены как стоп-слова
        lemmas.Should().HaveCount(2);
        lemmas.Should().ContainInOrder("test", "document");
    }

    [Fact]
    public void Lemmatize_ShouldHandleEmptyInput()
    {
        // Arrange
        var tokens = Array.Empty<string>();

        // Act
        var lemmas = _processor.Lemmatize(tokens);

        // Assert
        lemmas.Should().BeEmpty();
    }

    [Fact]
    public void PreprocessText_ShouldHandleEmptyString()
    {
        // Arrange
        var text = "";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void PreprocessText_ShouldHandleMixedLanguages()
    {
        // Arrange
        var text = "Hello мир! Это test на двух languages.";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        result.Should().Be("hello мир это test на двух languages");
    }
}