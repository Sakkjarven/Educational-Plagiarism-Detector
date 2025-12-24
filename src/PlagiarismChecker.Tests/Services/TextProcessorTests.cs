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
        // —лово "a" будет отфильтровано, так как оно короче 2 символов
        tokens.Should().HaveCount(5);
        tokens.Should().ContainInOrder("hello", "world", "this", "is", "test");
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
        // —лова "this", "is", "a" должны быть удалены как стоп-слова
        // "test" и "document" остаютс€ без изменений
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
        var text = "Hello мир! Ёто test на двух languages.";

        // Act
        var result = _processor.PreprocessText(text);

        // Assert
        // ”дал€ютс€ знаки препинани€, но сохран€ютс€ кириллические и латинские символы
        result.Should().Be("hello мир это test на двух languages");
    }

    [Fact]
    public void Tokenize_ShouldHandleCyrillic()
    {
        // Arrange
        var text = "привет мир это тест";

        // Act
        var tokens = _processor.Tokenize(text);

        // Assert
        tokens.Should().HaveCount(4);
        tokens.Should().ContainInOrder("привет", "мир", "это", "тест");
    }

    [Fact]
    public void Lemmatize_ShouldHandleCyrillicStopWords()
    {
        // Arrange
        var tokens = new[] { "это", "и", "тест", "дл€", "проверки" };

        // Act
        var lemmas = _processor.Lemmatize(tokens);

        // Assert
        // "это", "и", "дл€" - стоп-слова, должны быть удалены
        lemmas.Should().HaveCount(2);
        lemmas.Should().ContainInOrder("тест", "проверки");
    }
}