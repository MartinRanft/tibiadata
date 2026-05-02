using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.WheelOfDestiny;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.WheelOfDestiny.Interfaces;
using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.WheelOfDestiny
{
    public sealed class WheelDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IWheelDataBaseService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<Dictionary<WheelVocation, List<string>>> GetPerkNamesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "wheel:perk-names",
                async ct => await db.WheelPerks
                                    .AsNoTracking()
                                    .Where(x => x.IsActive)
                                    .GroupBy(x => x.Vocation)
                                    .ToDictionaryAsync(
                                        g => g.Key,
                                        g => g.Select(x => x.Name).OrderBy(n => n).ToList(),
                                        ct),
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<PagedResponse<WheelOfDestinyPerkListItemResponse>> GetPerksAsync(
            int page,
            int pageSize,
            string? vocation = null,
            string? type = null,
            string? search = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            string normalizedVocation = string.IsNullOrWhiteSpace(vocation) ? string.Empty : vocation.Trim().ToLowerInvariant();
            string normalizedType = string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim().ToLowerInvariant();
            string normalizedSearch = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
            string normalizedSort = string.IsNullOrWhiteSpace(sort) ? string.Empty : sort.Trim().ToLowerInvariant();
            WheelVocation? resolvedVocation = TryParseWheelVocation(normalizedVocation, out WheelVocation pv) ? pv : null;
            string vocationKey = resolvedVocation?.ToString().ToLowerInvariant() ?? string.Empty;
            string cacheKey = $"wheel:perks:{vocationKey}:{normalizedType}:{normalizedSearch}:{normalizedSort}:{descending}:{normalizedPage}:{normalizedPageSize}"
                .ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IQueryable<WheelPerk> query = db.WheelPerks
                                                    .AsNoTracking()
                                                    .Where(x => x.IsActive);

                    if(resolvedVocation.HasValue)
                    {
                        query = query.Where(x => x.Vocation == resolvedVocation.Value);
                    }
                    
                    if (!string.IsNullOrEmpty(normalizedType))
                    {
                        if(Enum.TryParse(normalizedType, ignoreCase: true, out WheelPerkType t))
                        {
                            query = query.Where(x => x.Type == t);          
                        }
                    }
                        
                    if (!string.IsNullOrEmpty(normalizedSearch))
                    {                                                                                                                                                                               
                        query = query.Where(x =>
                        x.Name.ToLower().Contains(normalizedSearch) ||
                        (x.Summary != null && x.Summary.ToLower().Contains(normalizedSearch)));                                                                                                 
                    }

                    int totalCount = await query.CountAsync(ct);

                    query = normalizedSort switch
                    {
                        "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                        "vocation" => descending ? query.OrderByDescending(x => x.Vocation) : query.OrderBy(x => x.Vocation),
                        "type" => descending ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
                        _ => query.OrderBy(x => x.Vocation).ThenBy(x => x.Type).ThenBy(x => x.Name)
                    };

                    List<WheelOfDestinyPerkListItemResponse> items = await query
                                                                           .Skip((normalizedPage - 1) * normalizedPageSize)
                                                                           .Take(normalizedPageSize)
                                                                           .Select(x => new WheelOfDestinyPerkListItemResponse(
                                                                               x.Id,
                                                                               x.Key,
                                                                               x.Slug,
                                                                               x.Vocation.ToString(),
                                                                               x.Type.ToString(),
                                                                               x.Name,
                                                                               x.Summary,
                                                                               x.IsGenericAcrossVocations,
                                                                               x.IsActive,
                                                                               x.MainSourceTitle,
                                                                               x.MainSourceUrl,
                                                                               x.LastUpdated))
                                                                           .ToListAsync(ct);
                    return new PagedResponse<WheelOfDestinyPerkListItemResponse>(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsByIdAsync(int perkId, CancellationToken cancellationToken = default)
        {
            if(perkId <= 0)
            {
                return null;
            }
            
            return await hybridCache.GetOrCreateAsync(
                $"wheel:perk-details:id:{perkId}",
                async ct =>
                {
                    WheelPerk? perk = await db.WheelPerks
                                             .AsNoTracking()
                                             .Include(x => x.Occurrences)
                                             .Include(x => x.Stages)
                                             .Where(x => x.Id == perkId)
                                             .FirstOrDefaultAsync(ct);

                    if(perk is null)
                    {
                        return null;
                    }
                    
                    return MapPerkDetails(perk);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsByKeyAsync(string perkKey, CancellationToken cancellationToken = default)
        {
            string normalizedKey = string.IsNullOrWhiteSpace(perkKey) ? string.Empty : perkKey.Trim().ToLowerInvariant();

            if(string.IsNullOrEmpty(normalizedKey))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"wheel:perk-details:key:{normalizedKey}",
                async ct =>
                {
                    WheelPerk? perk = await db.WheelPerks
                                              .AsNoTracking()
                                              .Include(x => x.Occurrences)
                                              .Include(x => x.Stages)
                                              .Where(x => x.Key == normalizedKey)
                                              .FirstOrDefaultAsync(ct);
                    
                    if(perk is null)
                    {
                        return null;
                    }
                    
                    return MapPerkDetails(perk);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsBySlugAsync(string slug, string vocation, CancellationToken cancellationToken = default)
        {
            string normalizedSlug = string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim().ToLowerInvariant();
            string normalizedVocation = string.IsNullOrWhiteSpace(vocation) ? string.Empty : vocation.Trim().ToLowerInvariant();

            if(string.IsNullOrEmpty(normalizedSlug) || string.IsNullOrEmpty(normalizedVocation))
            {
                return null;                                                                                                                                                                            
            }

            if(!TryParseWheelVocation(normalizedVocation, out WheelVocation parsedVocation))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"wheel:perk-details:slug:{normalizedSlug}:{normalizedVocation}",
                async ct =>
                {
                    WheelPerk? perk = await db.WheelPerks
                                              .AsNoTracking()
                                              .Include(x => x.Occurrences)
                                              .Include(x => x.Stages)
                                              .Where(x => x.Slug == normalizedSlug && x.Vocation == parsedVocation)
                                              .FirstOrDefaultAsync(ct);
                    
                    if(perk is null)
                    {
                        return null;
                    }
                    
                    return MapPerkDetails(perk);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<WheelOfDestinyLayoutResponse?> GetLayoutByVocationAsync(string vocation, CancellationToken cancellationToken = default)
        {
            string normalizedVocation = string.IsNullOrWhiteSpace(vocation) ? string.Empty : vocation.Trim().ToLowerInvariant();

            if(!TryParseWheelVocation(normalizedVocation, out WheelVocation parsedVocation))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"wheel:layout:{parsedVocation.ToString().ToLowerInvariant()}",
                async ct =>
                {
                    List<WheelSection> wheelSections = await db.WheelSections
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                                                        .Include(s => s.ConvictionWheelPerk)
                                                        .Include(s => s.ConvictionWheelPerkOccurrence)
                                                        .Include(s => s.DedicationPerks)
                                                        .ThenInclude(p => p.WheelPerk)
                                                        .Where(s => s.Vocation == parsedVocation)
                                                        .ToListAsync(ct);
                    
                    List<WheelRevelationSlot> wheelRevelation = await db.WheelRevelationSlots
                                                                        .AsNoTracking()
                                                                        .Include(r => r.WheelPerk)
                                                                        .Include(r => r.WheelPerkOccurrence)
                                                                        .Where(r => r.Vocation == parsedVocation)
                                                                        .ToListAsync(ct);
                    
                    return new WheelOfDestinyLayoutResponse(
                        normalizedVocation,
                        wheelSections.Select(w => new WheelOfDestinySectionResponse(
                            w.Id,
                            w.Vocation.ToString(),
                            w.SectionKey,
                            w.Quarter.ToString(),
                            w.RadiusIndex,
                            w.SortOrder,
                            w.SectionPoints,
                            MapPerkReference(w.ConvictionWheelPerk),
                            MapOccurrence(w.ConvictionWheelPerkOccurrence),
                            w.DedicationPerks.Select(d => new WheelOfDestinySectionDedicationPerkResponse(
                                d.Id,
                                d.SortOrder,
                                MapPerkReference(d.WheelPerk))).ToList()
                        )).ToList(),
                        wheelRevelation.Select(r => new WheelOfDestinyRevelationSlotResponse(
                            r.Id,
                            r.Vocation.ToString(),
                            r.SlotKey,
                            r.Quarter.ToString(),
                            r.RequiredPoints,
                            MapPerkReference(r.WheelPerk),
                            MapOccurrence(r.WheelPerkOccurrence))).ToList());
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<WheelOfDestinyGemResponse>> GetGemsAsync(string? vocation = null, CancellationToken cancellationToken = default)
        {
            string normalizedVocation = string.IsNullOrWhiteSpace(vocation)
            ? string.Empty
            : vocation.Trim().ToLowerInvariant();

            bool hasVocationInput = !string.IsNullOrWhiteSpace(normalizedVocation);

            GemVocation? parsedVocation = null;

            if (hasVocationInput)
            {
                bool parsed = TryParseGemVocation(normalizedVocation, out GemVocation vocationValue);

                if (!parsed)
                {
                    return [];
                }

                parsedVocation = vocationValue;
            }

            string cacheKey = parsedVocation is null ? "wheel:gems:all" : $"wheel:gems:{parsedVocation.Value}";

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IQueryable<Gem> query = db.Gems
                                              .AsNoTracking();

                    if (parsedVocation is not null)
                    {
                        query = query.Where(g => g.VocationRestriction == parsedVocation);
                    }

                    return await query
                                 .Select(g => new WheelOfDestinyGemResponse
                                 (
                                     g.Id,
                                     g.Name,
                                     g.WikiUrl,
                                     g.GemFamily.ToString(),
                                     g.GemSize.ToString(),
                                     g.VocationRestriction != null ? g.VocationRestriction.ToString() : null,
                                     g.Description,
                                     g.LastUpdated
                                 ))
                                 .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<PagedResponse<WheelOfDestinyGemModifierResponse>> GetGemModifiersAsync(
            int page,
            int pageSize,
            string? modifierType = null,
            string? category = null,
            string? vocation = null,
            string? search = null,
            bool? hasTradeoff = null,
            bool? isComboMod = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            string normalizedModifierType = string.IsNullOrWhiteSpace(modifierType) ? string.Empty : modifierType.Trim().ToLowerInvariant();
            string normalizedCategory = string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim().ToLowerInvariant();
            string normalizedVocation = string.IsNullOrWhiteSpace(vocation) ? string.Empty : vocation.Trim().ToLowerInvariant();
            string normalizedSearch = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
            string normalizedSort = string.IsNullOrWhiteSpace(sort) ? string.Empty : sort.Trim().ToLowerInvariant();
            bool normalizedHasTradeoff = hasTradeoff ?? false;
            bool normalizedIsComboMod = isComboMod ?? false;
            GemVocation? resolvedVocation = TryParseGemVocation(normalizedVocation, out GemVocation pgv) ? pgv : null;
            string vocationKey = resolvedVocation?.ToString().ToLowerInvariant() ?? string.Empty;
            string cacheKey =
            $"wheel:gem-modifiers:{normalizedModifierType}:{normalizedCategory}:" +
            $"{vocationKey}:{normalizedSearch}:{normalizedSort}:{descending}:" +
            $"{normalizedPage}:{normalizedPageSize}:{normalizedHasTradeoff}:{normalizedIsComboMod}";

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IQueryable<GemModifier> query = db.GemModifiers
                                                   .AsNoTracking()
                                                   .Include(x => x.Grades);

                    if(!string.IsNullOrEmpty(normalizedModifierType))
                    {
                        if(Enum.TryParse(normalizedModifierType, ignoreCase:true, out GemModifierType t))
                        {
                            query = query.Where(x => x.ModifierType == t);
                        }
                    }
                    
                    if(!string.IsNullOrEmpty(normalizedCategory))
                    {
                        if(Enum.TryParse(normalizedCategory, ignoreCase:true, out GemModifierCategory t))
                        {
                            query = query.Where(x => x.Category == t);
                        }
                    }
                    
                    if(resolvedVocation.HasValue)
                    {
                        query = query.Where(x => x.VocationRestriction == resolvedVocation.Value);
                    }
                    
                    if(!string.IsNullOrEmpty(normalizedSearch))
                    {
                        query = query.Where(x => x.Name.ToLower().Contains(normalizedSearch));
                    }

                    if(normalizedHasTradeoff)
                    {
                        query = query.Where(x => x.HasTradeoff == normalizedHasTradeoff);
                    }
                    
                    if(normalizedIsComboMod)
                    {
                        query = query.Where(x => x.IsComboMod == normalizedIsComboMod);
                    }
                    
                    int totalCount = await query.CountAsync(ct);

                    query = normalizedSort switch
                    {
                        "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                        "vocation" => descending ? query.OrderByDescending(x => x.VocationRestriction) : query.OrderBy(x => x.VocationRestriction),
                        "type" => descending ? query.OrderByDescending(x => x.ModifierType) : query.OrderBy(x => x.ModifierType),
                        "category" => descending ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
                        _ => query.OrderBy(x => x.ModifierType).ThenBy(x => x.Category).ThenBy(x => x.Name)
                    };

                    List<GemModifier> rawItems = await query
                                                       .Skip((normalizedPage - 1) * normalizedPageSize)
                                                       .Take(normalizedPageSize)
                                                       .ToListAsync(ct);

                    List<WheelOfDestinyGemModifierResponse> items = rawItems
                        .Select(x => new WheelOfDestinyGemModifierResponse(
                            x.Id,
                            x.Name,
                            x.VariantKey,
                            x.WikiUrl,
                            x.ModifierType.ToString(),
                            x.Category.ToString(),
                            x.VocationRestriction?.ToString(),
                            x.IsComboMod,
                            x.HasTradeoff,
                            x.Description,
                            x.Grades.Select(g => new WheelOfDestinyGemModifierGradeResponse(
                                g.Id,
                                g.Grade switch { GemGrade.GradeI => "I", GemGrade.GradeII => "II", GemGrade.GradeIII => "III", _ => "IV" },
                                g.ValueText,
                                g.ValueNumeric,
                                g.IsIncomplete,
                                g.LastUpdated)).ToList(),
                            x.LastUpdated))
                        .ToList();

                    return new PagedResponse<WheelOfDestinyGemModifierResponse>(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetPerkSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "sync:wheel:perks",
                async ct =>
                {
                    return await db.WheelPerks
                                   .AsNoTracking()
                                   .Where(x => x.IsActive)
                                   .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetPerkSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"sync:wheel:perks:{time}",
                async ct =>
                {
                    return await db.WheelPerks
                                   .AsNoTracking()
                                   .Where(x => x.IsActive)
                                   .Where(x => x.LastUpdated >= time)
                                   .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetGemSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "sync:wheel:gem",
                async ct =>
                {
                    return await db.Gems
                                   .AsNoTracking()
                                   .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetGemSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"sync:wheel:gem:{time}",
                async ct =>
                {
                    return await db.Gems
                                   .AsNoTracking()
                                   .Where(x => x.LastUpdated >= time)
                                   .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetGemModifierSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "sync:wheel:gemmodifiers",
                async ct =>
                {
                    return await db.GemModifiers
                                    .AsNoTracking()
                                    .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                    .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetGemModifierSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"sync:wheel:gemmodifiers:{time:O}",
                async ct =>
                {
                    return await db.GemModifiers
                                   .AsNoTracking()
                                   .Where(x => x.LastUpdated >= time)
                                   .Select(x => new SyncStateResponse(x.Id, x.LastUpdated, null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.WheelOfDestiny],
                cancellationToken);
        }

        private static WheelOfDestinyPerkReferenceResponse MapPerkReference(WheelPerk perk) =>
            new(perk.Id, perk.Key, perk.Slug, perk.Vocation.ToString(), perk.Type.ToString(), perk.Name);

        private static WheelOfDestinyPerkOccurrenceResponse? MapOccurrence(WheelPerkOccurrence? occurrence) =>
            occurrence is null ? null : new(occurrence.Id, occurrence.Domain, occurrence.OccurrenceIndex, occurrence.RequiredPoints, occurrence.IsStackable, occurrence.Notes);

        private static WheelOfDestinyPerkDetailsResponse MapPerkDetails(WheelPerk perk) =>
            new(perk.Id, perk.Key, perk.Slug, perk.Vocation.ToString(), perk.Type.ToString(),
                perk.Name, perk.Summary, perk.Description, perk.MainSourceTitle, perk.MainSourceUrl,
                perk.IsGenericAcrossVocations, perk.IsActive, perk.MetadataJson,
                [..perk.Occurrences.Select(o => new WheelOfDestinyPerkOccurrenceResponse(o.Id, o.Domain, o.OccurrenceIndex, o.RequiredPoints, o.IsStackable, o.Notes))],
                [..perk.Stages.Select(s => new WheelOfDestinyPerkStageResponse(s.Id, s.Stage, s.UnlockKind.ToString(), s.UnlockValue, s.EffectSummary, s.EffectDetailsJson, s.SortOrder))],
                perk.LastUpdated);

        private static string NormalizeVocationAlias(string s) => s switch
        {
            "ek" or "knight" or "elite-knight" or "eliteknight" => "knight",
            "rp" or "paladin" or "royal-paladin" or "royalpaladin" => "paladin",
            "ed" or "druid" or "elder-druid" or "elderdruid" => "druid",
            "ms" or "sorc" or "sorcerer" or "master-sorcerer" or "mastersorcerer" => "sorcerer",
            "em" or "monk" or "exalted-monk" or "exaltedmonk" => "monk",
            _ => s
        };

        private static bool TryParseWheelVocation(string input, out WheelVocation result)
        {
            string expanded = NormalizeVocationAlias(input) switch
            {
                "knight"   => "EliteKnight",
                "paladin"  => "RoyalPaladin",                                                                                                                                                                              
                "druid"    => "ElderDruid",
                "sorcerer" => "MasterSorcerer",                                                                                                                                                                            
                "monk"     => "ExaltedMonk",
                var s      => s                                                                                                                                                                                            
            };
            return Enum.TryParse(expanded, ignoreCase: true, out result);
        }
        
        private static bool TryParseGemVocation(string input, out GemVocation result)
            => Enum.TryParse(NormalizeVocationAlias(input), ignoreCase: true, out result);
    }
}