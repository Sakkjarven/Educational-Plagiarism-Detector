using MathNet.Numerics.LinearAlgebra;
using System.Collections.Immutable;
using System.Numerics;

namespace PlagiarismChecker.Core.Services;

public class TfIdfVectorizer
{
    private Dictionary<string, int> _vocabulary;
    private int _documentCount;
    private Dictionary<string, double> _idfCache;

    public TfIdfVectorizer()
    {
        _vocabulary = new Dictionary<string, int>();
        _documentCount = 0;
        _idfCache = new Dictionary<string, double>();
    }

    public void Fit(IEnumerable<IEnumerable<string>> documents)
    {
        var allTokens = documents.SelectMany(d => d).Distinct().ToList();
        _vocabulary = allTokens.Select((token, index) => new { token, index })
                              .ToDictionary(x => x.token, x => x.index);

        _documentCount = documents.Count();
        CalculateIdf(documents);
    }

    private void CalculateIdf(IEnumerable<IEnumerable<string>> documents)
    {
        foreach (var term in _vocabulary.Keys)
        {
            var docsWithTerm = documents.Count(doc => doc.Contains(term));
            _idfCache[term] = Math.Log((double)_documentCount / (docsWithTerm + 1)) + 1;
        }
    }

    public MathNet.Numerics.LinearAlgebra.Vector<double> Transform(IEnumerable<string> tokens)
    {
        var vector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(_vocabulary.Count);

        // Вычисляем TF (частота термина)
        var termFrequencies = tokens
            .Where(token => _vocabulary.ContainsKey(token))
            .GroupBy(token => token)
            .ToDictionary(g => g.Key, g => (double)g.Count() / tokens.Count());

        // Применяем TF-IDF
        foreach (var (term, tf) in termFrequencies)
        {
            var idf = _idfCache.GetValueOrDefault(term, 1.0);
            var index = _vocabulary[term];
            vector[index] = tf * idf;
        }

        return vector;
    }

    public static double CosineSimilarity(MathNet.Numerics.LinearAlgebra.Vector<double> vectorA, MathNet.Numerics.LinearAlgebra.Vector<double> vectorB)
    {
        if (vectorA.Count == 0 || vectorB.Count == 0)
            return 0.0;

        var dotProduct = vectorA.DotProduct(vectorB);
        var normA = vectorA.L2Norm();
        var normB = vectorB.L2Norm();

        if (normA == 0 || normB == 0)
            return 0.0;

        return dotProduct / (normA * normB);
    }
}