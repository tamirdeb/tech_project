using NUnit.Framework;
using WikipediaAutomation.Tests.Pages;

namespace WikipediaAutomation.Tests.Tests;

[TestFixture]
public class MicrosoftDevToolsTests : BaseTest
{
    [Test]
    public async Task AllTechnologyNames_ShouldBeLinks()
    {
        var articlePage = new WikipediaArticlePage(Page);
        await articlePage.NavigateAsync();

        var items = await articlePage.GetMicrosoftDevToolsItemsAsync();
        Assert.That(items, Is.Not.Empty, "No items found in section");

        var validLinks = new List<string>();
        var failures = new List<string>();

        // Collect the results silently instead of printing a log per item
        foreach (var (name, isLink) in items)
        {
            if (isLink)
                validLinks.Add(name);
            else
                failures.Add(name);
        }

        // 1. Log the high-level summary
        Log($"Total items found: <b>{items.Count}</b> (Valid: {validLinks.Count}, Failures: {failures.Count})");

        // 2. Log the detailed valid items in a neat, scrollable collapsible block
        if (validLinks.Count > 0)
        {
            var validHtmlList = "<ul>" + string.Join("", validLinks.Select(link => $"<li style='margin-bottom: 2px;'>✓ {link}</li>")) + "</ul>";
            Log($"<details><summary><b>Click to view all {validLinks.Count} verified technology links</b></summary><div style='color:#a0a0a0; font-size:13px; margin-top:8px; max-height:200px; overflow-y:auto; border: 1px solid #444; padding: 10px; border-radius: 5px;'>{validHtmlList}</div></details>");
        }

        // 3. Log failures prominently if there are any
        if (failures.Count > 0)
        {
            Log($"<b style='color:#ff4d4d;'>Failed items (Not a link):</b><br/><span style='color:#ff4d4d;'>{string.Join("<br/>✗ ", failures)}</span>");
        }

        // 4. Assert
        Assert.That(failures, Is.Empty,
            $"These items are not links:\n  {string.Join("\n  ", failures)}");
    }
}