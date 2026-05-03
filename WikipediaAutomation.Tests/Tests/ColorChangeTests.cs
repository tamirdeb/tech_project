using NUnit.Framework;
using WikipediaAutomation.Tests.Pages;

namespace WikipediaAutomation.Tests.Tests;

[TestFixture]
public class ColorChangeTests : BaseTest
{
    [Test]
    public async Task SwitchToDark_ShouldApplyDarkMode()
    {
        var articlePage = new WikipediaArticlePage(Page);
        await articlePage.NavigateAsync();

        // Capture and log the initial state in a single row
        var bgBefore = await articlePage.GetBodyBackgroundColorAsync();
        Log($"<b>Initial State (Light Mode):</b> Background Color <span style=\"background-color: {bgBefore}; width: 15px; height: 15px; display: inline-block; border: 1px solid #ccc; vertical-align: middle; margin-left: 5px; margin-right: 5px; border-radius: 3px;\"></span> <code>{bgBefore}</code>");

        Log("<i>Action: Toggled 'Dark' mode via the side menu...</i>");
        await articlePage.SelectDarkModeAsync();

        // Wait for CSS changes to apply
        await Page.WaitForTimeoutAsync(1000);

        // Capture and log the final state in a single row
        var isDark = await articlePage.IsDarkModeActiveAsync();
        var bgAfter = await articlePage.GetBodyBackgroundColorAsync();

        Log($"<b>Final State (Dark Mode):</b> Background Color <span style=\"background-color: {bgAfter}; width: 15px; height: 15px; display: inline-block; border: 1px solid #ccc; vertical-align: middle; margin-left: 5px; margin-right: 5px; border-radius: 3px;\"></span> <code>{bgAfter}</code> (Indicator Present: <b>{isDark}</b>)");

        if (bgAfter != bgBefore && isDark)
        {
            Log("<span style='color: #4CAF50;'><b>✓ Validation:</b> Background color successfully changed to dark, and dark mode class was found.</span>");
        }

        Assert.That(isDark, Is.True, "Dark mode indicator not found after toggle");
        Assert.That(bgAfter, Is.Not.EqualTo(bgBefore), "Background color did not change");
    }
}