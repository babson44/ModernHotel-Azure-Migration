# ModernHotel — Azure Migration Demo

A hands-on demo that mirrors the **Microsoft Cloud Workshop: Line-of-Business App Migration** lab, using a modern 3-tier hotel booking application. Deploy a simulated on-premises environment to Azure in one click, then practice migrating it using Azure Migrate and Azure Database Migration Service.

---

## 🚀 Deploy the Simulated On-Premises Environment

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fbabson44%2FModernHotel-Azure-Migration%2Fmain%2Finfra%2Fazuredeploy.json)

**What this deploys:**

A Windows Server 2022 Azure VM running **nested Hyper-V**, which automatically provisions three nested VMs that simulate an on-premises datacenter:

| Nested VM | Role | Stack |
|---|---|---|
| `hotel-sql` | Data tier | SQL Server 2016 + GrandAzureHotel DB |
| `hotel-api` | App tier | ASP.NET Core Web API (.NET 10) |
| `hotel-web` | Web tier | ASP.NET Core MVC frontend (.NET 10) |

Access to the Hyper-V host is via **Azure Bastion (Basic)** — secure browser-based RDP, no public IP exposed on the VM.

> ⏱️ **Estimated deployment time: ~60 minutes** — the VM deploys in ~5 minutes, then a background script spends ~55 minutes downloading Windows Server 2019, creating the three nested VMs, and installing SQL Server 2016 + the hotel application inside them.

---

## Deployment Parameters

| Parameter | Default | Description |
|---|---|---|
| `adminUsername` | `hotelAdmin` | Admin user for the host VM and all nested VMs |
| `adminPassword` | *(required)* | Min 12 chars — must include upper, lower, digit, and special character |
| `vmSize` | `Standard_D8s_v3` | Must be Dv3/Ev3/Dsv3 family for nested virtualization support |
| `prefix` | `modernhotel` | Prefix applied to all resource names |

---

## After Deployment — How to Connect

1. In the Azure portal, navigate to your **Resource Group**
2. Open the VM named **`modernhotel-hyperv-host`**
3. Click **Connect → Bastion**
4. Enter your `adminUsername` and `adminPassword`
5. A browser-based RDP session opens — no VPN or RDP client needed

Once connected, open **Hyper-V Manager** to see the three running nested VMs.

---

## Resources Deployed

```
Resource Group
├── modernhotel-vnet            Virtual Network (10.0.0.0/16)
│   ├── modernhotel-subnet      VM subnet (10.0.1.0/24)
│   └── AzureBastionSubnet      Bastion subnet (10.0.2.0/26)
├── modernhotel-nsg             Network Security Group (HTTP/HTTPS inbound)
├── modernhotel-bastion         Azure Bastion — Basic SKU
├── modernhotel-bastion-pip     Standard Public IP (for Bastion only)
├── modernhotel-hyperv-host     Windows Server 2022 VM (Standard_D8s_v3)
│   ├── OS disk                 256 GB Premium SSD
│   └── Data disk               512 GB Premium SSD (nested VM VHDs)
└── modernhotel-nic             Network Interface (private IP only)
```

> 💰 **Estimated monthly cost: ~$500–$600 USD** (dominated by the D8s_v3 VM + Bastion).
> **Delete the resource group when not in use** to avoid charges.

---

## Migration Walkthrough (Phase 3)

After the environment is running, practice migrating it:

1. **Azure Migrate: Server Assessment** — discover and assess `hotel-sql`, `hotel-api`, `hotel-web`
2. **Azure Migrate: Server Migration** — replicate and migrate the web/API tiers to Azure VMs
3. **Azure Database Migration Service** — migrate SQL Server 2016 → Azure SQL Database using Data Migration Assistant

> Full step-by-step migration docs are coming in Phase 3.

---

## Repo Layout

```
/app        ASP.NET Core 3-tier hotel application
/infra      Bicep/ARM template — nested Hyper-V + Azure Bastion
/scripts    Bootstrap PowerShell (runs inside the VM automatically after deploy)
/docs       Migration walkthrough (Phase 3 — planned)
```

---

## Phase Status

| Phase | Description | Status |
|---|---|---|
| 1 | Modern 3-tier hotel app (local Docker) | ✅ Complete |
| 2 | On-prem IaC — nested Hyper-V + Deploy-to-Azure | ✅ Complete |
| 3 | Azure Migrate + DMS walkthrough docs | 🔜 Planned |
| 4 | AI agent layer (post-migration modernization) | 🔜 Planned |

---

## Run the App Locally (Phase 1)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
cd app
docker compose up --build
```

The web frontend starts at **http://localhost:8080** and the API Swagger UI at **http://localhost:5000/swagger**.

**Stop and clean up:**
```bash
docker compose down -v
```

---

## The Hotel Application

**Grand Azure Hotel** — a 10-room hotel booking system seeded with realistic data:

- 📊 **Dashboard** — occupancy rate, revenue, today's arrivals and departures
- 📅 **Reservations** — full list with Check In / Check Out / Cancel actions
- 👤 **Guests** — guest directory with nationality and booking history
- 🚪 **Rooms** — room grid showing real-time availability

**Seed data:** 10 rooms (Standard $129 → Penthouse $999), 15 international guests, 20 reservations across all statuses.

---

## Build Verification

```bash
cd app
dotnet build ModernHotel.slnx
# Expected: Build succeeded. 0 Error(s)
```
