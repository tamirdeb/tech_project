using NUnit.Framework;
using WikipediaAutomation.Tests.Core;
using WikipediaAutomation.Tests.Pages;

namespace WikipediaAutomation.Tests.Tests;

[TestFixture]
public class DebuggingFeaturesTests : BaseTest
{
    [Test]
    public async Task UniqueWordCount_UI_And_API_ShouldMatch()
    {
        var articlePage = new WikipediaArticlePage(Page);
        await articlePage.NavigateAsync();

        Log("Extracting section text via UI...");
        var uiText = await articlePage.GetDebuggingFeaturesSectionTextAsync();
        Assert.That(uiText, Is.Not.Empty, "UI text was empty");

        using var api = new MediaWikiApiClient();
        Log("Extracting section text via API...");
        var apiText = await api.GetSectionPlainTextAsync("Debugging features");
        Assert.That(apiText, Is.Not.Empty, "API text was empty");

        var uiNorm = TextNormalizer.Normalize(uiText);
        var apiNorm = TextNormalizer.Normalize(apiText);

        // Extract the actual sets of words to display them in the report
        var uiWords = uiNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var apiWords = apiNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        Log($"UI unique words count: <b>{uiWords.Count}</b>");
        Log($"API unique words count: <b>{apiWords.Count}</b>");

        // The Magic Trick: Collapsible HTML logs for massive data dumping
        Log($"<details><summary><b>Click to view extracted UI words ({uiWords.Count} words)</b></summary><p style='color:#a0a0a0; font-size:13px; margin-top:5px;'>{string.Join(", ", uiWords)}</p></details>");
        Log($"<details><summary><b>Click to view extracted API words ({apiWords.Count} words)</b></summary><p style='color:#a0a0a0; font-size:13px; margin-top:5px;'>{string.Join(", ", apiWords)}</p></details>");

        var (onlyUi, onlyApi) = TextNormalizer.Diff(uiNorm, apiNorm);
        if (onlyUi.Count > 0) Log($"Only in UI: {string.Join(", ", onlyUi.Take(15))}");
        if (onlyApi.Count > 0) Log($"Only in API: {string.Join(", ", onlyApi.Take(15))}");

        string errorMessage = $"Word count mismatch — UI: {uiWords.Count}, API: {apiWords.Count}\n\n" +
                              $"---> WORDS ONLY IN UI: {string.Join(", ", onlyUi)}\n" +
                              $"---> WORDS ONLY IN API: {string.Join(", ", onlyApi)}\n";

        Assert.That(uiWords.Count, Is.EqualTo(apiWords.Count), errorMessage);
    }
}