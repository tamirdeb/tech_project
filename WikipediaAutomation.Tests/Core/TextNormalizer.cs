using System.Text.RegularExpressions;

namespace WikipediaAutomation.Tests.Core;

/// <summary>
/// Shared normalization pipeline — ensures UI and API text are comparable.
/// </summary>
public static class TextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var result = text.ToLowerInvariant();
        result = Regex.Replace(result, @"\[.*?\]", "");
        result = Regex.Replace(result, @"[^\w\s]", " ");
        result = Regex.Replace(result, @"\b\d+\b", "");
        result = Regex.Replace(result, @"\s+", " ");
        return result.Trim();
    }

    public static int CountUniqueWords(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText)) return 0;
        return normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet()
            .Count;
    }

    /// <summary>
    /// Symmetric diff — useful for diagnosing mismatches in test output.
    /// </summary>
    public static (HashSet<string> OnlyInA, HashSet<string> OnlyInB) Diff(string a, string b)
    {
        var setA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var onlyA = new HashSet<string>(setA); onlyA.ExceptWith(setB);
        var onlyB = new HashSet<string>(setB); onlyB.ExceptWith(setA);

        return (onlyA, onlyB);
    }
}
