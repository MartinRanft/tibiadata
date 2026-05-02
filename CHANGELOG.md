# Changelog

All notable changes to this project are documented in this file.


## [1.4.0] - 2026-05-02

### Added
- Added Wheel of Destiny perk catalog with occurrences, stages, and vocation assignments for all five vocations.
- Added official Wheel of Destiny layout data per vocation, including sections, dedication links, and revelation slots.
- Added Wheel of Destiny gem catalog with Lesser, Regular, and Greater variants for all five gem families.
- Added Wheel of Destiny modifier catalog with Basic Mods, Supreme Mods, grade values (I–IV), combo mods, tradeoff mods, and vocation-specific variants.
- Added public Wheel of Destiny response contracts for perks, layouts, sections, revelation slots, gems, and gem modifiers.


## [1.3.0] - 2026-04-23

### Added
- Added a public cross-resource search endpoint for common lookups by name or title.
- Added a lightweight asset metadata endpoint so clients can inspect mime type, dimensions, size, and checksum without downloading the binary.
- Added asset metadata search by file name for clients that already know asset file references.
- Added public metadata endpoints for `schemaVersion`, `dataVersion`, snapshot manifests, and centralized delta feeds.
- Added structured loot subresources for creatures by name and id.
- Added category lookups by category group to reduce client-side filtering.

### Changed
- Expanded `items` and `creatures` list endpoints with richer combined filters to reduce client request fan-out.
- Expanded creature detail responses with derived resistance summaries and normalized combat properties.
- Expanded hunting place detail responses with `name`, `implemented`, structured creature lists, and aggregated `areaCreatureSummaries`.
- Hunting place creature references now include `creatureId` so clients can jump directly into creature detail endpoints.
- Expanded book detail responses with structured page variants.
- Expanded quest detail responses with structured requirements and split rewards.
- Expanded building detail responses with structured addresses and normalized coordinates.

## [1.2.0] - 2026-04-09

### Added
- Added public `Bestiary` and `Bosstiary` API areas for category, points, and creature-based lookups.
- Added richer creature detail output through structured infobox data.
- Added raw infobox field exposure across public API responses to preserve the full scraped data set.

### Changed
- Expanded multiple public responses to expose more stored TibiaWiki data while keeping the existing TibiaData schema.
- Improved hunting place responses to include raw infobox fields for better parity and debugging.
- Expanded key detail responses to expose additional public fields such as aliases, origin, and split short/long notes.

### Fixed
- Fixed infobox parsing drift that could shift empty fields into the next key/value block.
- Fixed hunting place parsing artifacts caused by malformed infobox extraction.
- Fixed asset MIME-type detection by inspecting the actual file content instead of trusting stale metadata.

## [1.1.0] - 2026-04-08

### Added
- Added live, admin-managed request protection settings and editable rate limits in the admin panel.

### Changed
- Moved the public API docs to `/` for a cleaner default entry point.
- Improved reverse proxy handling for HTTPS-forwarded requests.

### Fixed
- Fixed root docs routing and production admin setup flow.
- Fixed admin request validation metadata for record-based request models.

## [1.0.0] - 2026-04-06

### Added
- Initial public release of TibiaData.
- Added the public REST API for TibiaWiki-backed Tibia data, including typed resource endpoints and sync endpoints.
- Added the admin panel, scraper controls, metrics, security hardening, and asset delivery endpoints.
- Added HybridCache and Redis-backed caching, background scraping, and operational monitoring.
