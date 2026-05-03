using Microsoft.Playwright;

namespace WikipediaAutomation.Tests.Pages;

public class WikipediaArticlePage
{
    private readonly IPage _page;
    private const string ArticleUrl = "https://en.wikipedia.org/wiki/Playwright_(software)";

    // Selectors
    private const string DebuggingFeaturesSectionSelector = "xpath=//h3[@id='Debugging_features']/parent::div/following-sibling::ul[1]";
    private const string MicrosoftDevToolsItemsSelector = "div.navbox:has-text('Microsoft development tools') td.navbox-list li";
    private const string DarkModeToggleSelector = "label:has-text('Dark')";
    private const string DarkModeActiveIndicatorSelector = "html[class*='night']";

    public WikipediaArticlePage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync(ArticleUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
    }

    /// <summary>
    /// Combines heading, intro paragraph, and bullet list into one string.
    /// Wikipedia wraps each part in separate sibling elements under the section anchor.
    /// </summary>
    public async Task<string> GetDebuggingFeaturesSectionTextAsync()
    {
        var heading = await _page.Locator("xpath=//h3[@id='Debugging_features']/parent::div").InnerTextAsync();
        var introParagraph = await _page.Locator("xpath=//h3[@id='Debugging_features']/parent::div/following-sibling::p[1]").InnerTextAsync();
        var listItems = await _page.Locator("xpath=//h3[@id='Debugging_features']/parent::div/following-sibling::ul[1]").InnerTextAsync();

        return $"{heading} {introParagraph} {listItems}";
    }

    /// <summary>
    /// Checks each navbox list item for the presence of an anchor tag.
    /// Skips empty structural elements.
    /// </summary>
    public async Task<List<(string Name, bool IsLink)>> GetMicrosoftDevToolsItemsAsync()
    {
        var items = _page.Locator(MicrosoftDevToolsItemsSelector);
        var count = await items.CountAsync();
        var results = new List<(string Name, bool IsLink)>();

        for (int i = 0; i < count; i++)
        {
            var item = items.Nth(i);
            var text = (await item.InnerTextAsync()).Trim();

            if (string.IsNullOrEmpty(text))
                continue;

            var linkCount = await item.Locator("a").CountAsync();
            results.Add((text, linkCount > 0));
        }

        return results;
    }

    public async Task SelectDarkModeAsync()
    {
        var toggle = _page.Locator(DarkModeToggleSelector);
        await toggle.ClickAsync();
    }

    /// <summary>
    /// check if dark mode is on.
    /// </summary>
    public async Task<bool> IsDarkModeActiveAsync()
    {
        var indicator = _page.Locator(DarkModeActiveIndicatorSelector);
        return await indicator.CountAsync() > 0;
    }

    /// <summary>
    /// No native Playwright API for computed styles; evaluate is required here.
    /// </summary>
    public async Task<string> GetBodyBackgroundColorAsync()
    {
        return await _page.Locator("body")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
    }
}
