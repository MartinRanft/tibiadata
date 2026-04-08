# TibiaDataApi

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![GitHub Actions](https://github.com/MartinRanft/tibiadata/actions/workflows/verify.yml/badge.svg)](https://github.com/MartinRanft/tibiadata/actions/workflows/verify.yml)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Redis](https://img.shields.io/badge/Redis-7.4-DC382D?logo=redis)](https://redis.io/)
[![MariaDB](https://img.shields.io/badge/MariaDB-Supported-003545?logo=mariadb)](https://mariadb.org/)

**High-performance REST API** providing structured Tibia game data from TibiaWiki with advanced caching, rate limiting, and real-time synchronization.

> **🌐 Live Demo:** [https://tibiadata.bytewizards.de/](https://tibiadata.bytewizards.de/)

---

## 🚀 Features

### Core Capabilities
- **42+ Endpoint Categories:** Items, Creatures, Hunting Places, Achievements, Books, Charms, NPCs, Spells, Quests, and more
- **Multi-Layer Caching:** HybridCache (L1 In-Memory + L2 Redis) with tag-based invalidation
- **Sync Endpoints:** Incremental updates via `/sync` and `/sync/by-date` for efficient client synchronization
- **Advanced Filtering:** Pagination, category filtering, name/ID lookups
- **Asset Streaming:** Optimized delivery of item and creature images
- **Real-time Scraping:** Background jobs sync data from TibiaWiki automatically

### Performance & Security
- ⚡ **Low-Latency Cached Reads:** Prepared data + HybridCache for fast public API responses
- 🔒 **Security Headers:** HSTS, CSP, X-Frame-Options, Referrer-Policy
- 🛡️ **Brute Force Protection:** Auto-ban after 5 failed login attempts
- 🔐 **PBKDF2-SHA256:** 100,000 iterations for password hashing
- 🚦 **Rate Limiting:** Token bucket algorithm with live editable policies in the admin panel
- 📊 **Prometheus Metrics:** Built-in metrics endpoint for monitoring

### Developer Experience
- 📖 **OpenAPI 3.0:** Full API documentation via Scalar UI
- 🐳 **Docker Ready:** Single-command deployment
- 🧪 **Integration Tests:** SQLite-based test suite
- 🔄 **Health Checks:** `/health/live` and `/health/ready` endpoints
- 🎯 **Structured Responses:** Consistent JSON DTOs across all endpoints

---

## 📋 Table of Contents

- [Quick Start](#-quick-start)
- [Tech Stack](#-tech-stack)
- [API Documentation](#-api-documentation)
- [Why TibiaData](#-why-tibiadata)
- [Development Setup](#️-development-setup)
- [Build & Test](#-build--test)
- [Configuration](#️-configuration)
- [Production Deployment](#-production-deployment)
- [Architecture](#-architecture)
- [Contributing](#-contributing)
- [License](#-license)

---

## ⚡ Quick Start

### Using Docker Compose (Recommended)

```bash
# Clone the repository
git clone <your-repo-url>
cd TibiaDataApi

# Start the API and Redis
docker compose -f compose.example.yaml up -d

# Access the API
open http://localhost:8096/
```

The API will be available at:
- **Scalar UI (API Explorer):** http://localhost:8096/
- **Health Check:** http://localhost:8096/health/ready
- **Prometheus Metrics:** http://localhost:8096/metrics (requires admin login)

### First-Time Setup

1. **Navigate to the API:** http://localhost:8096/
2. **Admin Setup:** On first launch, you'll be prompted to create an admin password
3. **Explore the API:** Use the Scalar UI to test endpoints interactively

---

## 🛠️ Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Runtime** | .NET | 10.0 |
| **Database** | MariaDB / MySQL | 10.6+ / 8.0+ |
| **Cache** | Redis | 7.4 |
| **API Framework** | ASP.NET Core | 10.0 |
| **ORM** | Entity Framework Core | 10.0 |
| **Caching** | HybridCache | Built-in |
| **Documentation** | Scalar UI | 2.13.18 |
| **Background Jobs** | Coravel | Latest |
| **Metrics** | Prometheus.NET | Latest |

---

## 📖 API Documentation

### Interactive Documentation

The API provides a **Scalar UI** interface for interactive exploration:

- **Public API:** http://localhost:8096/ (or https://tibiadata.bytewizards.de/)
- **Admin API:** http://localhost:8096/scalar/admin (requires authentication)

### Example Endpoints

#### Items
```http
GET /api/v1/items/list                    # List all item names
GET /api/v1/items?page=1&pageSize=100    # Paginated items
GET /api/v1/items/{name}                 # Item details by name
GET /api/v1/items/{id}                   # Item details by ID
GET /api/v1/items/categories             # All item categories
GET /api/v1/items/categories/{category}  # Items by category
GET /api/v1/items/sync                   # Sync state (all items)
GET /api/v1/items/sync/by-date?time=...  # Incremental sync
```

#### Creatures
```http
GET /api/v1/creatures/list               # List all creature names
GET /api/v1/creatures/{name}             # Creature details by name
GET /api/v1/creatures/{id}               # Creature details by ID
GET /api/v1/creatures/sync               # Sync state (all creatures)
GET /api/v1/creatures/sync/by-date?time=...
```

#### Other Resources
- **Hunting Places:** `/api/v1/hunting-places/*`
- **Achievements:** `/api/v1/achievements/*`
- **Charms:** `/api/v1/charms/*`
- **Books:** `/api/v1/books/*`
- **NPCs:** `/api/v1/npcs/*`
- **Spells:** `/api/v1/spells/*`
- **Quests:** `/api/v1/quests/*`
- **Mounts:** `/api/v1/mounts/*`
- **Outfits:** `/api/v1/outfits/*`

### Response Format

All responses follow a consistent structure:

```json
{
  "id": 123,
  "name": "Dragon Scale Mail",
  "wikiUrl": "https://tibia.fandom.com/wiki/Dragon_Scale_Mail",
  "lastUpdated": "2026-04-06T12:00:00Z",
  "structuredData": {
    "template": "Infobox_Item",
    "infobox": { ... }
  }
}
```

---

## 💡 Why TibiaData

TibiaData is not just another wrapper around TibiaWiki pages. It is designed as a stable, consumer-friendly data API for apps, bots, websites, mirrors, and tooling.

- **Structured over raw:** Clients receive typed DTOs instead of having to parse wiki markup, HTML, or mixed infobox data.
- **Sync-friendly:** `/sync` and `/sync/by-date` endpoints make it practical to keep local mirrors and caches up to date.
- **Operationally controlled:** Scraping, caching, metrics, bans, rate limits, and scheduled jobs are managed from one admin area.
- **Faster client integration:** Consumers can focus on features instead of building their own scraper, parser, cache, and image pipeline.
- **Independent data control:** The API persists and serves its own curated data model instead of depending on live page parsing for every request.

---

## 🖥️ Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for Redis and MariaDB)
- [Git](https://git-scm.com/)
- IDE: [Rider](https://www.jetbrains.com/rider/), [Visual Studio 2022](https://visualstudio.microsoft.com/), or [VS Code](https://code.visualstudio.com/)

### Local Development

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd TibiaDataApi

# 2. Start dependencies (Redis + MariaDB)
docker compose -f compose.example.yaml up -d tibiadataapi.redis

# 3. Restore dependencies
dotnet restore

# 4. Run the API
cd TibiaDataApi.Api
dotnet run

# The API will start at http://localhost:5000
```

### Environment Variables

Create a `appsettings.Development.json` or set environment variables:

```json
{
  "ConnectionStrings": {
    "DatabaseConnectionDev": "Server=localhost;Port=3306;Database=tibiadata;User=root;Password=yourpassword;charset=utf8mb4;",
    "Redis": "localhost:6379"
  },
  "AdminAccess": {
    "SessionHours": 24
  }
}
```

---

## 🧪 Build & Test

### Build Solution

```bash
# Build all projects
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test TibiaDataApi.Services.Tests
```

### Docker Build

```bash
# Build Docker image
docker build -t tibiadataapi:latest -f TibiaDataApi.Api/Dockerfile .

# Run Docker container
docker run -p 8080:8080 \
  -e ConnectionStrings__Redis=host.docker.internal:6379 \
  -e ConnectionStrings__DatabaseConnectionDev="Server=host.docker.internal;..." \
  tibiadataapi:latest
```

---

## ⚙️ Configuration

### appsettings.json Overview

```json
{
  "Database": {
    "Provider": "MariaDb",  // MariaDb, MySql, or Sqlite
    "ProductionConnectionStringName": "DatabaseConnection",
    "DevelopmentConnectionStringName": "DatabaseConnectionDev"
  },
  "Caching": {
    "UseRedisForHybridCache": true,
    "HybridCache": {
      "DefaultExpirationSeconds": 300,
      "DefaultLocalExpirationSeconds": 60
    }
  },
  "RequestProtection": {
    "PublicApi": {
      "TokenLimit": 120,
      "ReplenishmentSeconds": 60
    }
  }
}
```

### Key Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `Database__Provider` | Database type (MariaDb/MySql) | MariaDb |
| `Caching__UseRedisForHybridCache` | Enable Redis L2 cache | true |
| `RequestProtection__Enabled` | Enable rate limiting | true |
| `AdminAccess__SessionHours` | Admin session duration | 24 |
| `BackgroundJobs__ScheduledScraper__Enabled` | Auto-scraping | true |

### Rate Limiting

Rate limits are configured per endpoint category:

- **Public API:** 120 requests/minute
- **Admin Read:** 60 requests/minute
- **Admin Mutations:** 12 requests/minute
- **Health Checks:** 24 requests/minute

These limits can also be reviewed and adjusted live from the admin panel without restarting the API.

---

## 🚀 Production Deployment

### Docker Compose (Production)

```yaml
services:
  tibiadataapi.api:
    image: your-registry/tibiadataapi:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DatabaseConnection: "Server=db.prod;..."
      ConnectionStrings__Redis: "redis.prod:6379"
    ports:
      - "8080:8080"
    restart: unless-stopped
```

### Recommended Setup

1. **Reverse Proxy:** Nginx or Traefik
2. **Database:** MariaDB 10.6+ or MySQL 8.0+ (managed or self-hosted)
3. **Cache:** Redis 7.4+ (managed or self-hosted)
4. **Monitoring:** Prometheus + Grafana
5. **SSL/TLS:** Let's Encrypt via reverse proxy

### Admin Password Setup

**Option 1: First-Launch Wizard**
- Navigate to `/admin` after deployment
- Follow the setup wizard to create a password

**Option 2: Recovery Console**
```bash
# Run the recovery console
docker exec -it <container-name> dotnet TibiaDataApi.Api.dll admin reset-password

# Follow the prompts to reset the admin password
```

**Option 3: Environment Variable (Development Only)**
```bash
# NOT recommended for production!
ASPNETCORE_ENVIRONMENT=Development
# Default password: TibiaDataApiDev! (fixed in development mode)
```

### Health Checks

Configure health check endpoints for orchestration:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

---

## 🏗️ Architecture

### Project Structure

```
TibiaDataApi/
├── TibiaDataApi.Api/              # ASP.NET Core API
│   ├── Controller/                # API Controllers
│   ├── Middleware/                # Security, Rate Limiting
│   └── AdminAccess/               # Admin Dashboard
├── TibiaDataApi.Services/         # Business Logic
│   ├── DataBaseService/           # 42+ Database Services
│   ├── Scraper/                   # TibiaWiki Scrapers
│   ├── Assets/                    # Image Management
│   ├── Admin/                     # Security & Monitoring
│   └── Caching/                   # Cache Management
├── TibiaDataApi.Contracts/        # DTOs & Response Models
├── TibiaDataApi.Api.Tests/        # API Integration Tests
└── TibiaDataApi.Services.Tests/   # Service Unit Tests
```

### Data Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP Request
       ▼
┌─────────────────────────────┐
│   Rate Limiter              │
│   Security Headers          │
│   IP Ban Middleware         │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Controller                │
│   (Validation)              │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   HybridCache               │
│   (L1: In-Memory)           │
│   (L2: Redis)               │
└──────────┬──────────────────┘
           │ Cache Miss
           ▼
┌─────────────────────────────┐
│   Database Service          │
│   (EF Core + MariaDB)       │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Response DTO              │
│   (JSON Serialization)      │
└──────────┬──────────────────┘
           │
           ▼
       Client
```

### Background Jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| **TibiaScraperJob** | Every 1 minute | Sync data from TibiaWiki |
| **ItemImageSyncJob** | Every 10 minutes | Download item images |
| **CreatureImageSyncJob** | Every 10 minutes | Download creature images |

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Code Standards

- Follow [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Write unit tests for new features
- Update documentation for API changes
- Run `dotnet format` before committing

---

## 📊 Performance Notes

Performance depends heavily on deployment topology, cache warm-up, database provider, reverse proxy setup, and whether responses are served from hot cache.

The API is optimized for:

- cached read-heavy workloads
- structured DTO responses instead of raw wiki parsing on every request
- incremental synchronization via `/sync` and `/sync/by-date`
- low operational overhead through Redis-backed HybridCache

---

## 🔒 Security

### Reporting Vulnerabilities

Please report security vulnerabilities via email (do not create public issues).

### Security Features

- ✅ PBKDF2-SHA256 password hashing (100,000 iterations)
- ✅ Brute force protection (5 failed attempts = 20-minute ban)
- ✅ HSTS, CSP, X-Frame-Options headers
- ✅ CSRF protection (Antiforgery tokens)
- ✅ Rate limiting per endpoint
- ✅ IP-based access control
- ✅ HttpOnly, Secure, SameSite cookies

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **TibiaWiki** - Source of game data
- **Tibia Community** - Feedback and support
- **Scalar** - Beautiful API documentation UI
- **Prometheus** - Metrics and monitoring

---

## 📞 Support

- **Live Demo:** [https://tibiadata.bytewizards.de/](https://tibiadata.bytewizards.de/)
- **Issues:** Use the issue tracker on the platform where this repository is hosted
- **Documentation:** Available in Scalar UI at `/`

---

<p align="center">
  <strong>Built with ❤️ using .NET 10 and modern best practices</strong>
</p>
