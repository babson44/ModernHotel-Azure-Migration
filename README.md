# ModernHotel — Azure Migration & AI Modernization Demo

A self-contained demo that mirrors the Microsoft Cloud Workshop *Line-of-Business App
Migration* lab, but with a **lighter, modern 3-tier hotel application**. It lets you:

1. Deploy a fake **"on-premises"** environment into Azure (nested Hyper-V VMs) with **one click**.
2. **Migrate** it to Azure using **Azure Migrate** (Server Assessment + Server Migration)
   and **Azure Database Migration Service** (SQL Server 2016 → Azure SQL Database).
3. **Modernize** the migrated app by adding an **AI agent** (Azure OpenAI / Foundry) on top.

## Architecture

### "On-premises" environment (source)
A single Azure VM running **nested Hyper-V**, hosting the app tiers as nested VMs — so
Azure Migrate discovers and replicates them exactly like a real on-prem datacenter.

| Nested VM    | Role     | Stack                                              |
|--------------|----------|----------------------------------------------------|
| `hotel-web`  | Web tier | ASP.NET Core MVC (.NET 10) frontend                |
| `hotel-api`  | App tier | ASP.NET Core Web API (.NET 10)                     |
| `hotel-sql`  | Data tier| **SQL Server 2016** + seeded GrandAzureHotel DB    |

### Target (landing zone)
- App tiers  → Azure VMs (via Azure Migrate: Server Migration)
- SQL Server 2016 → Azure SQL Database (via DMS + Data Migration Assistant)

## Repo layout

```
/app        ASP.NET Core web + API + EF Core migrations + seed data
/infra      Bicep/ARM: nested Hyper-V host, landing zone, Deploy-to-Azure button  (Phase 2)
/scripts    Provisioning scripts that build the nested VMs and deploy the app      (Phase 2)
/docs       Migration walkthrough                                                   (Phase 3)
/ai         AI agent layer                                                          (Phase 4)
```

## Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Modern 3-tier hotel app | ✅ Complete |
| 2 | On-prem IaC (nested Hyper-V) + Deploy-to-Azure button | 🔜 Next |
| 3 | Migration walkthrough docs | 🔜 Planned |
| 4 | AI agent (post-migration modernization) | 🔜 Planned |

---

## Phase 1 — Quick Start

### Option A: Docker (recommended — no local .NET or SQL Server needed)

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

```bash
cd app
docker compose up --build
```

| Service | URL |
|---------|-----|
| 🌐 Web frontend | http://localhost:8080 |
| 🔌 API (Swagger) | http://localhost:5000/swagger |
| 🗄️ SQL Server | localhost:1433 (sa / Hotel@2025!) |

> First run takes ~3-5 minutes to pull images, build, and apply migrations.

**Stop everything:**
```bash
docker compose down
```

**Stop and wipe the database:**
```bash
docker compose down -v
```

---

### Option B: Without Docker (local .NET + SQL Server)

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) · SQL Server (any edition) or SQL Server Express

**1. Set connection string**

```powershell
# Windows PowerShell
$env:ConnectionStrings__HotelDb = "Server=localhost;Database=GrandAzureHotel;User Id=sa;Password=<yourpassword>;TrustServerCertificate=True;"
```

**2. Run the API** (applies migrations + seed automatically)
```bash
cd app/src/ModernHotel.API
dotnet run
# API starts at http://localhost:5000 — Swagger at http://localhost:5000/swagger
```

**3. Run the Web frontend** (in a second terminal)
```bash
cd app/src/ModernHotel.Web
$env:ApiBaseUrl = "http://localhost:5000/"
dotnet run
# Web starts at http://localhost:5001
```

---

## Application overview

**Grand Azure Hotel** — a 10-room, 15-guest hotel booking system with:

- 📊 **Dashboard** — occupancy rate, revenue this month, today's arrivals/departures
- 📅 **Reservations** — full list with Check In / Check Out / Cancel actions
- 👤 **Guests** — guest directory with nationality and booking history
- 🚪 **Rooms** — room grid showing real-time availability (Available / Occupied)
- 🔌 **API** — REST API with Swagger UI for direct exploration

### Default seed data
- **10 rooms** across 4 types: Standard ($129), Deluxe ($199), Suite ($349-$499), Penthouse ($799-$999)
- **15 international guests** from 12 countries
- **20 reservations** with varied statuses: Checked In, Checked Out, Confirmed, Pending, Cancelled

---

## Build verification

```bash
cd app
dotnet build ModernHotel.slnx     # Should report: Build succeeded. 0 Error(s)
```

