using System.Text;
using System.Text.RegularExpressions;

namespace PlagiarismChecker.Core.Services;

public class TextProcessor : ITextProcessor
{
    private readonly HashSet<string> _stopWords;

    // ѕростой список стоп-слов дл€ английского и русского
    private static readonly string[] DefaultStopWords =
    {
        // јнглийские
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "is", "are", "was", "were", "be", "been", "being","this",
        
        // –усские
        "это","и", "в", "во", "не", "что", "он", "на", "€", "с", "со", "как", "а",
        "то", "все", "она", "так", "его", "но", "да", "ты", "к", "у", "же",
        "вы", "за", "бы", "по", "только", "ее", "мне", "было", "вот", "от",
        "мен€", "еще", "нет", "о", "из", "ему", "теперь", "когда", "даже",
        "ну", "вдруг", "ли", "если", "уже", "или", "ни", "быть", "был", "него",
        "до", "вас", "нибудь", "уж", "вам", "сказал", "ведь", "там", "потом",
        "себ€", "ничего", "ей", "может", "они", "тут", "где", "есть", "надо",
        "ней", "дл€", "мы", "теб€", "их", "чем", "была", "сам", "чтоб", "без",
        "будто", "чего", "раз", "тоже", "себе", "под", "будет", "ж", "тогда",
        "кто", "этот", "того", "потому", "этого", "какой", "совсем", "ним",
        "здесь", "этом", "один", "почти", "мой", "тем", "чтобы", "нее", "сейчас",
        "были", "куда", "зачем", "всех", "никогда", "можно", "при", "наконец",
        "два", "об", "другой", "хоть", "после", "над", "больше", "тот", "через",
        "эти", "нас", "про", "всего", "них", "кака€", "много", "разве", "три",
        "эту", "мо€", "впрочем", "хорошо", "свою", "этой", "перед", "иногда",
        "лучше", "чуть", "том", "нельз€", "такой", "им", "более", "всегда",
        "конечно", "всю", "между"
    };

    public TextProcessor()
    {
        _stopWords = new HashSet<string>(DefaultStopWords, StringComparer.OrdinalIgnoreCase);
    }

    public string PreprocessText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // ѕриводим к нижнему регистру
        text = text.ToLowerInvariant();

        // ”дал€ем HTML/XML теги
        text = Regex.Replace(text, @"<[^>]+>|&nbsp;|&amp;|&lt;|&gt;", " ");

        // ”дал€ем URL
        text = Regex.Replace(text, @"https?://\S+|www\.\S+", " ");

        // ”дал€ем email
        text = Regex.Replace(text, @"\S+@\S+\.\S+", " ");

        // ќставл€ем только буквы (включа€ кириллицу), цифры, пробелы и дефисы
        // »спользуем Unicode категории дл€ поддержки кириллицы
        text = Regex.Replace(text, @"[^\p{L}\p{N}\s\-]", " ");

        // «амен€ем множественные пробелы на один
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    public string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        // –аздел€ем по не-буквенным символам (включа€ Unicode буквы)
        var tokens = Regex.Split(text, @"[^\p{L}]+", RegexOptions.Compiled)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Where(t => t.Length > 1) // ‘ильтруем слишком короткие токены
            .ToArray();

        return tokens;
    }

    public string[] Lemmatize(string[] tokens)
    {
        if (tokens == null || tokens.Length == 0)
            return Array.Empty<string>();

        var lemmas = new List<string>();

        foreach (var token in tokens)
        {
            // ѕропускаем стоп-слова
            if (_stopWords.Contains(token))
                continue;

            // ”прощенна€ лемматизаци€ - оставл€ем как есть
            // ¬ реальном проекте нужно использовать библиотеку
            lemmas.Add(token);
        }

        return lemmas.ToArray();
    }
}