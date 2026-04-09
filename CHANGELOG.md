# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

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
