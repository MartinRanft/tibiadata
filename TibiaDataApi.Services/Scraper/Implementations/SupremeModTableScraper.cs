using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper.Implementations;

public sealed class SupremeModTableScraper(
    ITibiaWikiHttpService tibiaWikiHttpService,
    ILogger<SupremeModTableScraper> logger)
{
    private const string SupremeModPageTitle = "Supreme Mod";

    public async Task<List<ModDraft>> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Scraping Supreme Mod data from TibiaWiki page: {PageTitle}", SupremeModPageTitle);

        string rawWikiText = await LoadRawWikiTextAsync(SupremeModPageTitle, cancellationToken);
        List<ModDraft> drafts = ParseModTables(rawWikiText);

        logger.LogInformation("Scraped {Count} Supreme Mods from TibiaWiki.", drafts.Count);

        return drafts;
    }

    private async Task<string> LoadRawWikiTextAsync(string pageTitle, CancellationToken cancellationToken)
    {
        string requestUri =
            $"/api.php?action=query&prop=revisions&rvslots=main&rvprop=content&formatversion=2&format=json&titles={Uri.EscapeDataString(pageTitle)}";

        string responseJson = await tibiaWikiHttpService.GetStringAsync(requestUri, cancellationToken);
        using JsonDocument document = JsonDocument.Parse(responseJson);

        JsonElement pages = document.RootElement
                                    .GetProperty("query")
                                    .GetProperty("pages");

        foreach (JsonElement page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("revisions", out JsonElement revisions) || revisions.GetArrayLength() == 0)
            {
                continue;
            }

            return revisions[0]
                   .GetProperty("slots")
                   .GetProperty("main")
                   .GetProperty("content")
                   .GetString() ?? string.Empty;
        }

        throw new InvalidOperationException($"TibiaWiki returned no raw wiki text for '{pageTitle}'.");
    }

    private static List<ModDraft> ParseModTables(string rawWikiText)
    {
        List<ModDraft> drafts = [];

        foreach ((string title, string content) in ParseSections(rawWikiText))
        {
            GemVocation? vocation = title switch
            {
                "General" => null,
                "Knights" => GemVocation.Knight,
                "Druids" => GemVocation.Druid,
                "Paladins" => GemVocation.Paladin,
                "Sorcerers" => GemVocation.Sorcerer,
                "Monks" => GemVocation.Monk,
                _ => null
            };

            if (title is not ("General" or "Knights" or "Druids" or "Paladins" or "Sorcerers" or "Monks"))
            {
                continue;
            }

            Dictionary<string, int> ordinalByKey = new(StringComparer.OrdinalIgnoreCase);
            List<IReadOnlyList<string>> rows = ParseFirstWikiTable(content);

            foreach (IReadOnlyList<string> row in rows)
            {
                if (LooksLikeHeaderRow(row))
                {
                    continue;
                }

                string firstCell = CleanWikiText(row[0]);

                string ordinalSeed = $"{title}|{vocation?.ToString() ?? "general"}|{firstCell}";
                ordinalByKey.TryGetValue(ordinalSeed, out int seenCount);
                int ordinal = seenCount + 1;
                ordinalByKey[ordinalSeed] = ordinal;

                drafts.Add(new ModDraft(
                    VariantKey: BuildVariantKey(title, firstCell, vocation, ordinal),
                    Name: firstCell,
                    Type: GemModifierType.Supreme,
                    Category: vocation is null ? GemModifierCategory.General : GemModifierCategory.VocationSpecific,
                    VocationRestriction: vocation,
                    GradeValues: new Dictionary<GemGrade, string>
                    {
                        [GemGrade.GradeI] = CleanWikiText(row[1]),
                        [GemGrade.GradeII] = CleanWikiText(row[2]),
                        [GemGrade.GradeIII] = CleanWikiText(row[3]),
                        [GemGrade.GradeIV] = CleanWikiText(row[4])
                    },
                    IsCombo: firstCell.Contains('/'),
                    HasTradeoff: false,
                    Description: null));
            }
        }

        return drafts;
    }

    private static bool LooksLikeHeaderRow(IReadOnlyList<string> row)
    {
        if (row.Count < 5)
        {
            return true;
        }

        string firstCell = CleanWikiText(row[0]);
        if (string.Equals(firstCell, "Mod", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(firstCell, "Augments", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(firstCell) ||
            string.Equals(firstCell, "[[]]", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(CleanWikiText(row[1]), "Grade I", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(CleanWikiText(row[2]), "Grade II", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(CleanWikiText(row[3]), "Grade III", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(CleanWikiText(row[4]), "Grade IV", StringComparison.OrdinalIgnoreCase);
    }

    private static List<(string Title, string Content)> ParseSections(string text)
    {
        List<(string Title, string Content)> sections = [];
        string normalized = NormalizeTableSyntax(text);
        MatchCollection matches = SectionHeadingRegex.Matches(normalized);

        for (int index = 0; index < matches.Count; index++)
        {
            Match match = matches[index];
            int contentStart = match.Index + match.Length;
            int contentEnd = index + 1 < matches.Count ? matches[index + 1].Index : normalized.Length;
            string title = CleanWikiText(match.Groups["title"].Value);
            string content = normalized[contentStart..contentEnd];
            sections.Add((title, content));
        }

        return sections;
    }

    private static List<IReadOnlyList<string>> ParseFirstWikiTable(string text)
    {
        Match match = WikiTableRegex.Match(NormalizeTableSyntax(text));
        if (!match.Success)
        {
            return [];
        }

        string table = match.Value;
        List<List<string>> rows = [];
        List<string> currentRowLines = [];

        foreach (string rawLine in table.Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.StartsWith("{|", StringComparison.Ordinal) || line.StartsWith("|}", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("|-", StringComparison.Ordinal))
            {
                if (currentRowLines.Count > 0)
                {
                    rows.Add(ParseTableRow(currentRowLines));
                    currentRowLines = [];
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                currentRowLines.Add(line);
            }
        }

        if (currentRowLines.Count > 0)
        {
            rows.Add(ParseTableRow(currentRowLines));
        }

        return rows
               .Where(entry => entry.Count > 0)
               .Cast<IReadOnlyList<string>>()
               .ToList();
    }

    private static string BuildVariantKey(string sectionTitle, string name, GemVocation? vocation, int ordinal)
    {
        string normalizedName = VariantKeyNonAlphaNumericRegex
                                .Replace(CleanWikiText(name).ToLowerInvariant(), "-")
                                .Trim('-');
        string normalizedSection = VariantKeyNonAlphaNumericRegex
                                   .Replace(sectionTitle.ToLowerInvariant(), "-")
                                   .Trim('-');
        string vocationPart = vocation?.ToString().ToLowerInvariant() ?? "general";

        return $"supreme-{normalizedSection}-{vocationPart}-{normalizedName}-{ordinal}";
    }

    private static List<string> ParseTableRow(IReadOnlyList<string> rowLines)
    {
        List<string> cells = [];

        foreach (string rowLine in rowLines)
        {
            if (rowLine.StartsWith("!", StringComparison.Ordinal))
            {
                cells.AddRange(ParseHeaderCells(rowLine));
                continue;
            }

            if (rowLine.StartsWith("|", StringComparison.Ordinal))
            {
                cells.AddRange(ParseDataCells(rowLine));
            }
        }

        return cells
               .Select(CleanWikiText)
               .Where(value => !string.IsNullOrWhiteSpace(value))
               .ToList();
    }

    private static IEnumerable<string> ParseHeaderCells(string rowLine)
    {
        return rowLine[1..]
               .Split("!!", StringSplitOptions.None)
               .Select(ExtractCellValue);
    }

    private static IEnumerable<string> ParseDataCells(string rowLine)
    {
        foreach (string part in rowLine[1..].Split("||", StringSplitOptions.None))
        {
            yield return ExtractCellValue(part);
        }
    }

    private static string ExtractCellValue(string rawCell)
    {
        string cell = rawCell.Trim();
        int pipeIndex = FindTopLevelPipeIndex(cell);
        if (pipeIndex >= 0)
        {
            string trailingValue = cell[(pipeIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(trailingValue))
            {
                return trailingValue;
            }
        }

        return cell;
    }

    private static int FindTopLevelPipeIndex(string value)
    {
        int wikiLinkDepth = 0;
        int templateDepth = 0;

        for (int index = 0; index < value.Length; index++)
        {
            if (index < value.Length - 1)
            {
                if (value[index] == '[' && value[index + 1] == '[')
                {
                    wikiLinkDepth++;
                    index++;
                    continue;
                }

                if (value[index] == ']' && value[index + 1] == ']')
                {
                    wikiLinkDepth = Math.Max(0, wikiLinkDepth - 1);
                    index++;
                    continue;
                }

                if (value[index] == '{' && value[index + 1] == '{')
                {
                    templateDepth++;
                    index++;
                    continue;
                }

                if (value[index] == '}' && value[index + 1] == '}')
                {
                    templateDepth = Math.Max(0, templateDepth - 1);
                    index++;
                    continue;
                }
            }

            if (value[index] == '|' && wikiLinkDepth == 0 && templateDepth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormalizeTableSyntax(string text)
    {
        return text.Replace("{{{!}}", "{|", StringComparison.Ordinal)
                   .Replace("{{!}}", "|", StringComparison.Ordinal);
    }

    private static string CleanWikiText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        string cleaned = NormalizeTableSyntax(rawText);
        cleaned = WikiLinkRegex.Replace(cleaned, match => match.Groups["label"].Success
            ? match.Groups["label"].Value
            : match.Groups["target"].Value);
        cleaned = SimpleTemplateRegex.Replace(cleaned, string.Empty);
        cleaned = cleaned.Replace("'''", string.Empty, StringComparison.Ordinal)
                         .Replace("''", string.Empty, StringComparison.Ordinal);
        cleaned = BreakRegex.Replace(cleaned, "; ");
        cleaned = HtmlTagRegex.Replace(cleaned, " ");
        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = cleaned.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                         .Replace("\r", " ", StringComparison.Ordinal)
                         .Replace("\n", " ", StringComparison.Ordinal);

        return WhitespaceRegex.Replace(cleaned, " ").Trim(' ', ':', ';');
    }

    private static readonly Regex SectionHeadingRegex = new(@"^(?<markers>={2,6})\s*(?<title>.*?)\s*\k<markers>$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex WikiTableRegex = new(@"\{\|[\s\S]*?\|\}", RegexOptions.Compiled);
    private static readonly Regex WikiLinkRegex = new(@"\[\[(?<target>[^\]|]+)(?:\|(?<label>[^\]]+))?\]\]", RegexOptions.Compiled);
    private static readonly Regex SimpleTemplateRegex = new(@"\{\{[^{}]*\}\}", RegexOptions.Compiled);
    private static readonly Regex BreakRegex = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex VariantKeyNonAlphaNumericRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);
}
