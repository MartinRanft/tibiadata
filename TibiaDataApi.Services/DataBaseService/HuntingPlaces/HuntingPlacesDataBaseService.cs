using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.HuntingPlaces;
using TibiaDataApi.Contracts.Public.WikiArticles;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.HuntingPlaces.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.HuntingPlaces
{
    public sealed partial class HuntingPlacesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IHuntingPlacesDataBaseService
    {
        private const string HuntingPlacesCategorySlug = "hunting-places";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places-list",
                async cancel =>
                {
                    List<HuntingPlaceListReadModel> articles = await db.WikiArticles
                                                                       .AsNoTracking()
                                                                       .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                                       .Where(x => !x.IsMissingFromSource)
                                                                       .OrderBy(x => x.Title)
                                                                       .Select(x => new HuntingPlaceListReadModel(
                                                                           x.Id,
                                                                           x.Title,
                                                                           x.Summary,
                                                                           x.InfoboxJson,
                                                                           x.WikiUrl,
                                                                           x.LastUpdated))
                                                                       .ToListAsync(cancel);

                    return articles.Select(MapListItem)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByNameAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(huntingPlaceName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-details-by-name:{normalizedName}",
                async cancel =>
                {
                    int articleId = await db.WikiArticles
                                            .AsNoTracking()
                                            .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                            .Where(x => !x.IsMissingFromSource)
                                            .Where(x => x.NormalizedTitle == normalizedName)
                                            .Select(x => x.Id)
                                            .SingleOrDefaultAsync(cancel);

                    return articleId <= 0 ? null : await GetHuntingPlaceDetailsByIdAsync(articleId, cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByIdAsync(
            int huntingPlaceId,
            CancellationToken cancellationToken = default)
        {
            if(huntingPlaceId <= 0)
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-details:{huntingPlaceId}",
                async cancel =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                   .Where(x => x.Id == huntingPlaceId)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .FirstOrDefaultAsync(cancel);

                    if(article is null)
                    {
                        return null;
                    }

                    List<WikiArticleCategoryResponse> articleCategories = await db.WikiArticleCategories
                                                                                  .AsNoTracking()
                                                                                  .Where(c => c.WikiArticleId == article.Id)
                                                                                  .Where(c => c.WikiCategory != null && !c.IsMissingFromSource)
                                                                                  .Select(c => new WikiArticleCategoryResponse(
                                                                                      c.WikiCategoryId,
                                                                                      c.WikiCategory!.Slug,
                                                                                      c.WikiCategory.Name,
                                                                                      c.WikiCategory.GroupSlug,
                                                                                      c.WikiCategory.GroupName))
                                                                                  .ToListAsync(cancel);

                    return await MapDetailsAsync(article, articleCategories, cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceAreaRecommendationResponse?> GetHuntingPlaceAreaRecommendationAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(huntingPlaceName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-area-recommendation-by-name:{normalizedName}",
                async cancel =>
                {
                    HuntingPlaceAreaReadModel? article = await db.WikiArticles
                                                                 .AsNoTracking()
                                                                 .Where(x => x.NormalizedTitle == normalizedName)
                                                                 .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                                 .Where(x => !x.IsMissingFromSource)
                                                                 .OrderBy(x => x.Id)
                                                                 .Select(x => new HuntingPlaceAreaReadModel(
                                                                     x.InfoboxJson,
                                                                     x.AdditionalAttributesJson))
                                                                 .FirstOrDefaultAsync(cancel);

                    if(article is null)
                    {
                        return null;
                    }

                    HuntingPlaceAdditionalAttributes? additionalAttributes = DeserializeAdditionalAttributes(article.AdditionalAttributesJson);
                    List<HuntingPlaceAreaRecommendationResponse> lowerLevels = BuildLowerLevels(additionalAttributes);
                    if(lowerLevels.Count > 0)
                    {
                        return lowerLevels[0];
                    }

                    HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);
                    return infobox is null ? null : CreateAreaRecommendation(infobox);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetHuntingPlaceUpdates(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places:sync-states",
                async cancel =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken
            );
        }
        public async Task<List<SyncStateResponse>> GetHuntingPlaceUpdatesByDate(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places:sync-states-byDate",
                async cancel =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken
            );
        }

        private async Task<HuntingPlaceDetailsResponse> MapDetailsAsync(
            WikiArticle article,
            IReadOnlyList<WikiArticleCategoryResponse> articleCategories,
            CancellationToken cancellationToken)
        {
            HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);
            HuntingPlaceAdditionalAttributes? additionalAttributes = DeserializeAdditionalAttributes(article.AdditionalAttributesJson);
            List<HuntingPlaceAreaRecommendationResponse> lowerLevels = BuildLowerLevels(additionalAttributes);
            List<HuntingPlaceAreaCreatureSummaryResponse> areaCreatureSummaries = await ParseAreaCreatureSummariesAsync(
                article.RawWikiText,
                additionalAttributes,
                article.Title,
                cancellationToken);
            HuntingPlaceStructuredDataResponse structuredData = new(
                article.InfoboxTemplate,
                infobox,
                additionalAttributes,
                areaCreatureSummaries);

            return new HuntingPlaceDetailsResponse(
                article.Id,
                article.Title,
                article.Title,
                article.Summary,
                article.PlainTextContent,
                article.RawWikiText,
                structuredData,
                infobox?.Image,
                infobox?.Implemented,
                infobox?.City,
                infobox?.Location,
                infobox?.Vocation,
                infobox?.LevelKnights,
                infobox?.LevelPaladins,
                infobox?.LevelMages,
                infobox?.SkillKnights,
                infobox?.SkillPaladins,
                infobox?.SkillMages,
                infobox?.DefenseKnights,
                infobox?.DefensePaladins,
                infobox?.DefenseMages,
                infobox?.Loot,
                infobox?.LootStar,
                infobox?.Experience,
                infobox?.ExperienceStar,
                infobox?.BestLoot,
                infobox?.BestLoot2,
                infobox?.BestLoot3,
                infobox?.BestLoot4,
                infobox?.BestLoot5,
                infobox?.Map,
                infobox?.Map2,
                infobox?.Map3,
                infobox?.Map4,
                BuildFlattenedCreatures(areaCreatureSummaries),
                lowerLevels,
                articleCategories,
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static HuntingPlaceListItemResponse MapListItem(HuntingPlaceListReadModel article)
        {
            HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);

            return new HuntingPlaceListItemResponse(
                article.Id,
                article.Title,
                article.Title,
                article.Summary,
                infobox?.City,
                infobox?.Location,
                infobox?.Vocation,
                article.WikiUrl,
                article.LastUpdated);
        }

        private static HuntingPlaceInfobox? DeserializeInfobox(string? infoboxJson)
        {
            if(string.IsNullOrWhiteSpace(infoboxJson))
            {
                return null;
            }

            try
            {
                HuntingPlaceInfobox? infobox = JsonSerializer.Deserialize<HuntingPlaceInfobox>(infoboxJson, JsonOptions);

                if(infobox is not null)
                {
                    infobox.Fields = StructuredJsonParser.ParseStringDictionary(infoboxJson);
                }

                return infobox;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HuntingPlaceAdditionalAttributes? DeserializeAdditionalAttributes(string? additionalAttributesJson)
        {
            if(string.IsNullOrWhiteSpace(additionalAttributesJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<HuntingPlaceAdditionalAttributes>(additionalAttributesJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static List<HuntingPlaceAreaRecommendationResponse> BuildLowerLevels(HuntingPlaceAdditionalAttributes? additionalAttributes)
        {
            return additionalAttributes?.LowerLevels?
                                       .Select(CreateAreaRecommendation)
                                       .ToList()
                   ?? [];
        }

        private static HuntingPlaceAreaRecommendationResponse CreateAreaRecommendation(HuntingPlaceLowerLevel lowerLevel)
        {
            return new HuntingPlaceAreaRecommendationResponse(
                lowerLevel.AreaName,
                lowerLevel.LevelKnights,
                lowerLevel.LevelPaladins,
                lowerLevel.LevelMages,
                lowerLevel.SkillKnights,
                lowerLevel.SkillPaladins,
                lowerLevel.SkillMages,
                lowerLevel.DefenseKnights,
                lowerLevel.DefensePaladins,
                lowerLevel.DefenseMages);
        }

        private static HuntingPlaceAreaRecommendationResponse CreateAreaRecommendation(HuntingPlaceInfobox infobox)
        {
            return new HuntingPlaceAreaRecommendationResponse(
                infobox.AreaName,
                infobox.LevelKnights,
                infobox.LevelPaladins,
                infobox.LevelMages,
                infobox.SkillKnights,
                infobox.SkillPaladins,
                infobox.SkillMages,
                infobox.DefenseKnights,
                infobox.DefensePaladins,
                infobox.DefenseMages);
        }

        private static IReadOnlyList<HuntingPlaceCreatureResponse> BuildFlattenedCreatures(
            IReadOnlyList<HuntingPlaceAreaCreatureSummaryResponse> areaCreatureSummaries)
        {
            if(areaCreatureSummaries.Count == 0)
            {
                return [];
            }

            List<HuntingPlaceCreatureResponse> creatures = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach(HuntingPlaceAreaCreatureSummaryResponse summary in areaCreatureSummaries)
            {
                foreach(HuntingPlaceCreatureResponse creature in summary.Creatures)
                {
                    if(!seen.Add(creature.Name))
                    {
                        continue;
                    }

                    creatures.Add(creature);
                }
            }

            return creatures;
        }

        private async Task<List<HuntingPlaceAreaCreatureSummaryResponse>> ParseAreaCreatureSummariesAsync(
            string? rawWikiText,
            HuntingPlaceAdditionalAttributes? additionalAttributes,
            string huntingPlaceName,
            CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(rawWikiText))
            {
                return [];
            }

            List<HuntingPlaceAreaCreatureSummaryResponse> summaries = [];
            string[] lines = rawWikiText.Split('\n');
            bool inCreaturesSection = false;
            string? currentSectionHeading = null;

            for(int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                (int level, string headingText)? heading = TryParseHeading(line);

                if(heading is not null)
                {
                    if(heading.Value.level == 2)
                    {
                        inCreaturesSection = string.Equals(heading.Value.headingText, "Creatures", StringComparison.OrdinalIgnoreCase);
                        currentSectionHeading = null;
                    }
                    else if(inCreaturesSection && heading.Value.level >= 3)
                    {
                        currentSectionHeading = heading.Value.headingText;
                    }

                    continue;
                }

                if(!line.StartsWith("{{CreatureList", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<string> blockLines = [line];

                while(index + 1 < lines.Length)
                {
                    if(lines[index].Contains("}}", StringComparison.Ordinal))
                    {
                        break;
                    }

                    index++;
                    blockLines.Add(lines[index].TrimEnd());

                    if(lines[index].Contains("}}", StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                List<HuntingPlaceCreatureResponse> blockCreatures = ParseCreatureBlockCreatures(blockLines);
                if(blockCreatures.Count == 0)
                {
                    continue;
                }

                string? caption = ExtractCreatureListCaption(blockLines);
                string areaName = ResolveAreaName(caption, currentSectionHeading, huntingPlaceName);
                HuntingPlaceLowerLevel? matchingArea = FindMatchingLowerLevel(additionalAttributes, areaName, currentSectionHeading);

                summaries.Add(new HuntingPlaceAreaCreatureSummaryResponse(
                    areaName,
                    currentSectionHeading,
                    blockCreatures.Count,
                    blockCreatures,
                    CreateVocationValues(matchingArea?.LevelKnights, matchingArea?.LevelPaladins, matchingArea?.LevelMages),
                    CreateVocationValues(matchingArea?.SkillKnights, matchingArea?.SkillPaladins, matchingArea?.SkillMages),
                    CreateVocationValues(matchingArea?.DefenseKnights, matchingArea?.DefensePaladins, matchingArea?.DefenseMages)));
            }

            return await ResolveCreatureIdsAsync(summaries, cancellationToken);
        }

        private static List<HuntingPlaceCreatureResponse> ParseCreatureBlockCreatures(IReadOnlyList<string> blockLines)
        {
            List<HuntingPlaceCreatureResponse> creatures = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach(string rawLine in blockLines)
            {
                string line = rawLine.Trim();

                if(!line.StartsWith("|", StringComparison.Ordinal))
                {
                    continue;
                }

                string value = line[1..].Trim();

                if(string.IsNullOrWhiteSpace(value) ||
                   value.Contains('='))
                {
                    continue;
                }

                string normalizedValue = NormalizeCreatureName(value);

                if(string.IsNullOrWhiteSpace(normalizedValue) ||
                   !seen.Add(normalizedValue))
                {
                    continue;
                }

                creatures.Add(new HuntingPlaceCreatureResponse(null, normalizedValue));
            }

            return creatures;
        }

        private async Task<List<HuntingPlaceAreaCreatureSummaryResponse>> ResolveCreatureIdsAsync(
            IReadOnlyList<HuntingPlaceAreaCreatureSummaryResponse> summaries,
            CancellationToken cancellationToken)
        {
            if(summaries.Count == 0)
            {
                return [];
            }

            HashSet<string> candidateNames = [];

            foreach(HuntingPlaceAreaCreatureSummaryResponse summary in summaries)
            {
                foreach(HuntingPlaceCreatureResponse creature in summary.Creatures)
                {
                    foreach(string candidate in GetCreatureLookupCandidates(creature.Name))
                    {
                        candidateNames.Add(candidate);
                    }
                }
            }

            if(candidateNames.Count == 0)
            {
                return summaries.ToList();
            }

            List<(int Id, string NormalizedName)> knownCreatures = await db.Creatures
                                                                           .AsNoTracking()
                                                                           .Select(x => new ValueTuple<int, string>(x.Id, x.NormalizedName))
                                                                           .ToListAsync(cancellationToken);

            Dictionary<string, int> creatureIdsByNormalizedName = new(StringComparer.OrdinalIgnoreCase);

            foreach((int creatureId, string normalizedName) in knownCreatures)
            {
                if(!candidateNames.Contains(normalizedName) ||
                   creatureIdsByNormalizedName.ContainsKey(normalizedName))
                {
                    continue;
                }

                creatureIdsByNormalizedName[normalizedName] = creatureId;
            }

            return summaries.Select(summary => summary with
                           {
                               Creatures = summary.Creatures
                                                  .Select(creature => creature with
                                                  {
                                                      CreatureId = ResolveCreatureId(creature.Name, creatureIdsByNormalizedName)
                                                  })
                                                  .ToList()
                           })
                           .ToList();
        }

        private static int? ResolveCreatureId(
            string creatureName,
            IReadOnlyDictionary<string, int> creatureIdsByNormalizedName)
        {
            foreach(string candidate in GetCreatureLookupCandidates(creatureName))
            {
                if(creatureIdsByNormalizedName.TryGetValue(candidate, out int creatureId))
                {
                    return creatureId;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCreatureLookupCandidates(string creatureName)
        {
            string normalizedName = EntityNameNormalizer.Normalize(creatureName);
            if(!string.IsNullOrWhiteSpace(normalizedName))
            {
                yield return normalizedName;
            }

            const string creatureSuffix = " (Creature)";
            if(creatureName.EndsWith(creatureSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string strippedName = creatureName[..^creatureSuffix.Length];
                string normalizedStrippedName = EntityNameNormalizer.Normalize(strippedName);
                if(!string.IsNullOrWhiteSpace(normalizedStrippedName) &&
                   !string.Equals(normalizedStrippedName, normalizedName, StringComparison.Ordinal))
                {
                    yield return normalizedStrippedName;
                }
            }
        }

        private static string NormalizeCreatureName(string value)
        {
            string normalized = value.Trim();

            normalized = normalized.Replace("[[", string.Empty, StringComparison.Ordinal)
                                   .Replace("]]", string.Empty, StringComparison.Ordinal);

            int pipeIndex = normalized.IndexOf('|');
            if(pipeIndex >= 0 && pipeIndex < normalized.Length - 1)
            {
                normalized = normalized[(pipeIndex + 1)..];
            }

            normalized = Regex.Replace(normalized, @"\{\{.*?\}\}", string.Empty, RegexOptions.Singleline);
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        private static string? ExtractCreatureListCaption(IReadOnlyList<string> blockLines)
        {
            foreach(string rawLine in blockLines)
            {
                Match match = CreatureListCaptionRegex().Match(rawLine);
                if(!match.Success)
                {
                    continue;
                }

                string value = match.Groups["value"].Value.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return null;
        }

        private static (int level, string headingText)? TryParseHeading(string line)
        {
            Match match = HeadingRegex().Match(line);
            if(!match.Success)
            {
                return null;
            }

            string headingText = match.Groups["text"].Value.Trim();
            if(string.IsNullOrWhiteSpace(headingText))
            {
                return null;
            }

            return (match.Groups["equals"].Value.Length, headingText);
        }

        private static string ResolveAreaName(string? caption, string? currentSectionHeading, string huntingPlaceName)
        {
            if(!string.IsNullOrWhiteSpace(caption))
            {
                return caption.Trim();
            }

            if(!string.IsNullOrWhiteSpace(currentSectionHeading))
            {
                return currentSectionHeading.Trim();
            }

            return huntingPlaceName;
        }

        private static HuntingPlaceLowerLevel? FindMatchingLowerLevel(
            HuntingPlaceAdditionalAttributes? additionalAttributes,
            string areaName,
            string? currentSectionHeading)
        {
            if(additionalAttributes?.LowerLevels is null || additionalAttributes.LowerLevels.Count == 0)
            {
                return null;
            }

            string normalizedAreaName = NormalizeAreaName(areaName);
            string normalizedSectionHeading = NormalizeAreaName(currentSectionHeading);

            foreach(HuntingPlaceLowerLevel lowerLevel in additionalAttributes.LowerLevels)
            {
                string normalizedLowerLevelName = NormalizeAreaName(lowerLevel.AreaName);

                if(string.IsNullOrWhiteSpace(normalizedLowerLevelName))
                {
                    continue;
                }

                if(string.Equals(normalizedLowerLevelName, normalizedAreaName, StringComparison.Ordinal) ||
                   (!string.IsNullOrWhiteSpace(normalizedSectionHeading) &&
                    string.Equals(normalizedLowerLevelName, normalizedSectionHeading, StringComparison.Ordinal)))
                {
                    return lowerLevel;
                }
            }

            return null;
        }

        private static string NormalizeAreaName(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value, @"[^a-z0-9]+", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                        .ToLowerInvariant();
        }

        private static HuntingPlaceVocationValueResponse? CreateVocationValues(
            string? knights,
            string? paladins,
            string? mages)
        {
            if(string.IsNullOrWhiteSpace(knights) &&
               string.IsNullOrWhiteSpace(paladins) &&
               string.IsNullOrWhiteSpace(mages))
            {
                return null;
            }

            return new HuntingPlaceVocationValueResponse(
                string.IsNullOrWhiteSpace(knights) ? null : knights.Trim(),
                string.IsNullOrWhiteSpace(paladins) ? null : paladins.Trim(),
                string.IsNullOrWhiteSpace(mages) ? null : mages.Trim());
        }

        private sealed record HuntingPlaceListReadModel(
            int Id,
            string Title,
            string? Summary,
            string? InfoboxJson,
            string? WikiUrl,
            DateTime LastUpdated);

        private sealed record HuntingPlaceAreaReadModel(
            string? InfoboxJson,
            string? AdditionalAttributesJson);

        [GeneratedRegex(@"\{\{CreatureList(?<content>.*?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex CreatureListBlockRegex();

        [GeneratedRegex(@"^(?<equals>=+)\s*(?<text>.*?)\s*\k<equals>$", RegexOptions.CultureInvariant, "en-US")]
        private static partial Regex HeadingRegex();

        [GeneratedRegex(@"(?:^|\|)\s*caption\s*=\s*(?<value>[^|}]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CreatureListCaptionRegex();
    }
}
