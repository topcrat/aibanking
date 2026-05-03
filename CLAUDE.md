# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend (AIBanking/)
```bash
# Run via Aspire orchestrator (recommended — starts API + dashboard)
cd AIBanking.AppHost && dotnet run

# Run API standalone
cd AIBanking && dotnet run

# Apply EF Core migrations
cd AIBanking && dotnet ef database update

# Add a new migration
cd AIBanking && dotnet ef migrations add <MigrationName>

# Build solution
dotnet build AIBanking.sln
```

### Frontend (AIBanking.Web/)
```bash
cd AIBanking.Web && npm install
cd AIBanking.Web && npm run dev      # Vite dev server (proxies /api → backend)
cd AIBanking.Web && npm run build
cd AIBanking.Web && npm run lint
```

### Running the full stack
Start the Aspire AppHost (`dotnet run` in `AIBanking.AppHost/`). The Aspire dashboard is at `http://localhost:15888`. The API is at `https://localhost:7164`. Scalar API docs (dev only) at `https://localhost:7164/scalar`.

If ports are already bound from a crashed session, find and kill the process:
```powershell
Get-NetTCPConnection -LocalPort 7164 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
```

## Architecture

### Projects
- **AIBanking** — ASP.NET Core 9 REST API (main backend)
- **AIBanking.AppHost** — .NET Aspire orchestration host; adds only the API project
- **AIBanking.Web** — React 18 + TypeScript + Vite SPA

### Backend layers

```
Controllers/          → Thin HTTP layer; delegate to services/agent
Services/             → Business logic (all registered as Singletons)
Agents/               → Claude AI agent wiring
  BankingAgentService.cs   ← single entry point for all AI chat
  Tools/                   ← AIFunctionFactory tools grouped by concern
    ApplicationQueryTools      read-only application queries
    ApplicationActionTools     create customer, create account, approve/reject
    ComplianceTools            query BVN/NIN verification records
    ComplianceActionTools      trigger BVN/NIN verification
    DocumentTools              list/download documents
Models/               → EF Core entities
DTOs/                 → Request/response shapes (separate from entities)
Data/
  BankingDbContext.cs
  Configurations/     → Fluent API config (one file per entity)
  Migrations/
Enums/                → C# enums (AccountStatus, KycTier, etc.)
```

### Key architectural patterns

**Singleton services + scoped DbContext**: All services are singletons. They receive `IDbContextFactory<BankingDbContext>` and call `await _dbFactory.CreateDbContextAsync()` at the top of each method, disposing the context at the end. Never inject `BankingDbContext` directly into a singleton.

**Agent tools with per-request DbContext**: `BankingAgentService.ChatAsync` creates a DI scope (`_sp.CreateScope()`), resolves a fresh `BankingDbContext` from it, and passes it when constructing all tool classes. This means every chat turn gets its own context that is disposed when the scope exits.

**AI tool registration pattern**:
```csharp
var tool = AIFunctionFactory.Create(methodName, description, options);
```
Tools are grouped into `*QueryTools` (read-only) and `*ActionTools` (writes) classes. Each class takes the DbContext + dependent services in its constructor. New tools must be registered in `BankingAgentService` and described in the system prompt.

**Session persistence**: Conversation state is stored in `ConcurrentDictionary<string, JsonElement>` keyed by `conversationId`. State is serialized to `JsonElement` between turns and deserialized at the start of each turn.

### Authentication
JWT Bearer, HS256, 8-hour expiry. Roles: `Admin`, `Staff`. Tokens issued by `POST /api/auth/login`. All application and workflow endpoints require authentication. `[Authorize(Roles = "Admin")]` gates admin-only operations.

### Database
PostgreSQL via Npgsql + EF Core 9. Table names are snake_case (configured in each `IEntityTypeConfiguration`). `ExtractedPersonInfo` is stored as a JSON column owned by `AccountApplication`.

### AI / Claude integration
Uses `Microsoft.Agents.AI.Anthropic 1.1.0-rc1` (`AnthropicClient`). Configured via `appsettings.json`:
```json
"Anthropic": { "ApiKey": "...", "DeploymentName": "claude-..." }
```
The `BankingAgentService` system prompt encodes the full account-opening workflow and all compliance rules. When adding a new workflow step, update both the tool methods and the system prompt.

### Stub services
BVN verification (`BvnVerificationService`), NIN verification (`NinVerificationService`), notifications (`NotificationService`) are production-ready stubs. Each has a comment block showing where to plug in the real provider (NIBSS, NIMC, Twilio, SendGrid, FCM).

### Fraud scoring
`FraudDetectionService` assigns a risk score (0–100+) from 10 rules. Rules 1–6 cover document extraction and BVN; Rules 7–10 cover NIN, consent, and PEP/sanctions checks. Threshold: ≥70 = Critical risk.

### KYC tiers (CBN-compliant)
Determined at account creation in `ApplicationActionTools.DetermineKycTier()`:
- **Tier 1**: BVN or NIN only → ₦50k/₦300k/₦300k (single/daily/max balance)
- **Tier 2**: (BVN or NIN) + address → ₦200k/₦1M/₦5M
- **Tier 3**: BVN + NIN + address + all bio fields → ₦10M/₦50M/unlimited

### Frontend structure
```
src/
  pages/          → Route-level components (Dashboard, ApplicationDetail, etc.)
  components/     → Shared UI components
  services/       → API client modules (api.ts, applicationService.ts, etc.)
  types/index.ts  → All TypeScript types (enums, models, DTOs)
  contexts/       → AuthContext (JWT storage + user state)
```

API base URL is configured in Vite proxy; all frontend API calls use relative `/api/...` paths.

## Known gaps (not yet implemented)
- `AccountApplicationResponse` DTO missing `BvnNumber`, `NinNumber`, `ConsentGiven` mapping
- No `PATCH /api/applications/{id}/consent` endpoint for NDPA consent capture
- `CheckApplicationStandardsAsync` does not gate approval on BVN/NIN completion
- `Selfie` document type not in `DocumentType` enum (required for digital onboarding BPMN)
- `Restricted` account status missing from `AccountStatus` enum
- `OnboardingChannel` (Branch/Digital/Mobile) not captured on the application
- Immutable audit log table absent
