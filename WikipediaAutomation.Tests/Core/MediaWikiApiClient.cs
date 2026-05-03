using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WikipediaAutomation.Tests.Core;

/// <summary>
/// Extracts article section text via the MediaWiki Parse API
/// </summary>
public class MediaWikiApiClient : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://en.wikipedia.org/w/api.php";
    private const string PageTitle = "Playwright_(software)";

    public MediaWikiApiClient()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "WikipediaAutomation/1.0");
    }

    // Gets the plain text content of a section by name, stripping out HTML and references for easier comparison with UI-extracted text
    public async Task<string> GetSectionPlainTextAsync(string sectionName)
    {
        var sectionsUrl = $"{BaseUrl}?action=parse&page={PageTitle}&prop=sections&format=json";
        var sectionsResp = await _http.GetFromJsonAsync<ParseResponse>(sectionsUrl);
        var section = sectionsResp?.Parse?.Sections?
            .FirstOrDefault(s => s.Line?.Equals(sectionName, StringComparison.OrdinalIgnoreCase) is true);

        if (section == null)
            throw new InvalidOperationException($"Section '{sectionName}' not found.");

        // disableeditsection=1 strips the [edit] links from the API response HTML
        var textUrl = $"{BaseUrl}?action=parse&page={PageTitle}&prop=text&section={section.Index}&format=json&disableeditsection=1";
        var textResp = await _http.GetFromJsonAsync<ParseResponse>(textUrl);
        var html = textResp?.Parse?.Text?["*"] ?? string.Empty;

        return StripHtml(html);
    }


    // an HTML stripper to get just the plain text content for comparison with UI-extracted text
    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Strip non-content blocks first to avoid leftover noise
        var text = Regex.Replace(html, @"<(style|script)[^>]*>[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<span[^>]*class=""[^""]*mw-editsection[^""]*""[^>]*>[\s\S]*?</span>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<ol[^>]*class=""[^""]*references[^""]*""[^>]*>[\s\S]*?</ol>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<div[^>]*class=""[^""]*mw-references-wrap[^""]*""[^>]*>[\s\S]*?</div>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", " ");
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\[\d+\]", "");
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    // IDisposable implementation to clean up HttpClient
    public void Dispose() => _http.Dispose();

    // Only used for deserialization
    private class ParseResponse
    {
        [JsonPropertyName("parse")] public ParseData? Parse { get; set; }
    }

    private class ParseData
    {
        [JsonPropertyName("sections")] public List<SectionDto>? Sections { get; set; }
        [JsonPropertyName("text")] public Dictionary<string, string>? Text { get; set; }
    }

    private class SectionDto
    {
        [JsonPropertyName("line")] public string? Line { get; set; }
        [JsonPropertyName("index")] public string? Index { get; set; }
    }
}
