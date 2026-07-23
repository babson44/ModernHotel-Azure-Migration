# ModernHotel — Azure Migration & AI Modernization Demo

A self-contained demo that mirrors the Microsoft Cloud Workshop *Line-of-Business App
Migration* lab, but with a **lighter, modern 3-tier hotel application**. It lets you:

1. Deploy a fake **"on-premises"** environment into Azure (nested Hyper-V VMs) with **one click**.
2. **Migrate** it to Azure using **Azure Migrate** (Server Assessment + Server Migration)
   and **Azure Database Migration Service** (SQL Server → Azure SQL Database).
3. **Modernize** the migrated app by adding an **AI agent** (Azure OpenAI / Foundry) on top.

## Architecture

### "On-premises" environment (source)
A single Azure VM running **nested Hyper-V**, hosting the app tiers as nested VMs — so
Azure Migrate discovers and replicates them exactly like a real on-prem datacenter.

| Nested VM        | Role        | Stack                                   |
|------------------|-------------|-----------------------------------------|
| `hotel-web`      | Web tier    | ASP.NET Core (Razor/Blazor) frontend    |
| `hotel-api`      | App tier    | ASP.NET Core Web API                     |
| `hotel-sql`      | Data tier   | SQL Server (Windows) + seeded DB         |

### Target (landing zone)
- App tiers  → Azure VMs (via Azure Migrate: Server Migration)
- SQL Server → Azure SQL Database (via DMS + Data Migration Assistant)

### Modernization (phase 3)
- Azure OpenAI / Foundry agent over the migrated hotel data
  (natural-language booking queries, a concierge/ops assistant).

## One-click "Deploy to Azure"
Works via the portal magic URL:
`https://portal.azure.com/#create/Microsoft.Template/uri/<URL-ENCODED-TEMPLATE-URL>`
The ARM/Bicep template is hosted at a public HTTPS URL; the portal renders the
Review+Create form from its parameters.

## Repo layout (planned)
```
/app        ASP.NET Core web + API + SQL schema/seed
/infra      Bicep/ARM: nested Hyper-V host, landing zone, Deploy-to-Azure button
/scripts    Provisioning scripts that build the nested VMs and deploy the app
/ai         Phase 3 — AI agent layer
/docs       Migration walkthrough
```

## Status
🚧 Scaffolding. Build sequence: (1) modern app → (2) on-prem IaC → (3) migration guide → (4) AI agent.
