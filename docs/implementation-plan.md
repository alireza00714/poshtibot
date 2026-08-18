# Namadno AI Support — Implementation Plan

Handover document for the Namadno backend team. This service is a **standalone bounded context**. It owns conversations, knowledge, AI orchestration, and support tickets created by AI Support. It does **not** own Namadno users, accounts, funds, payments, or financial execution.

**Runtime:** .NET 10 (`net10.0`), SDK pinned in `global.json`.

**Phase 0 result:** empty workspace. No existing Namadno backend source. No official FAQ answer document in the repo. Seed FAQ will use the supplied topic list with conservative answers only. Invented fees, processing times, and product policies are forbidden.

---

## 1. Architecture

Strict Clean Architecture. Dependency arrows point inward.

```
Api  →  Application  →  Domain
              ↑
        Infrastructure
```

| Layer | May depend on | Must not depend on |
|---|---|---|
| Domain | BCL only | Application, Infrastructure, API, EF Core, ASP.NET, PostgreSQL, LLM SDKs |
| Application | Domain | Infrastructure, API, EF Core, ASP.NET, HTTP clients, LM Studio |
| Infrastructure | Application, Domain | API project |
| API | Application, Infrastructure (composition root only) | Domain internals except via Application |

Infrastructure implements Application abstractions (`IChatModel`, repositories, Namadno HTTP adapters, tools).

**No MediatR.** Lightweight CQRS: explicit command/query handlers per feature folder.

**No god objects.** Chat path is a pipeline, not a single `ChatService`.

```
Request
  → IUserContext (trusted identity)
  → Conversation load + ownership
  → Persian normalization
  → Intent classification (structured, validated)
  → Policy evaluation
  → FAQ/RAG | controlled tool | navigation | human support
  → Response generation
  → Response validation
  → Persistence + audit metadata
  → API contract
```

Coordinators (Application):

- `ChatOrchestrator`
- `IntentClassifier`
- `PolicyEvaluator`
- `KnowledgeRetriever`
- `ToolDispatcher`
- `ResponseGenerator`
- `ResponseValidator`
- `ConversationManager`
- `SupportManager`

---

## 2. Projects

Solution: `Namadno.AI.Support.sln`

```
src/
  Namadno.AI.Support.Domain
  Namadno.AI.Support.Application
  Namadno.AI.Support.Infrastructure
  Namadno.AI.Support.Api
tests/
  Namadno.AI.Support.UnitTests
  Namadno.AI.Support.IntegrationTests
  Namadno.AI.Support.ArchitectureTests
  Namadno.AI.Support.EvaluationTests
```

| Project | Role |
|---|---|
| Domain | Entities, value objects, enums, domain events, invariants |
| Application | Use cases, DTOs, ports, options, policies, orchestration |
| Infrastructure | EF Core, pgvector, LLM, embeddings, HTTP Namadno clients, tools, jobs |
| Api | Controllers, auth, middleware, health, OpenAPI, composition root |
| UnitTests | Domain rules, normalizer, intent parse, policy, authz, navigation |
| IntegrationTests | PostgreSQL/pgvector (Testcontainers), API, auth middleware, support flow |
| ArchitectureTests | Layer dependency rules |
| EvaluationTests | ≥150 Persian cases from `persian-evaluation.json` |

---

## 3. Dependencies

Central package management (`Directory.Packages.props`).

| Concern | Package / mechanism |
|---|---|
| Web | ASP.NET Core 10 |
| ORM | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 |
| Vectors | `Pgvector` + `Pgvector.EntityFrameworkCore` |
| Validation | FluentValidation |
| Logging | Serilog |
| HTTP resilience | `Microsoft.Extensions.Http.Resilience` (retry only on safe reads) |
| Auth | JWT bearer + gateway/header strategies behind `IUserContext` |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` |
| Tests | xUnit, FluentAssertions, Testcontainers.PostgreSql, NetArchTest |
| Rate limit | ASP.NET Core built-in rate limiter |
| Health | ASP.NET health checks; readiness includes PostgreSQL |

Domain has **zero** NuGet packages.

---

## 4. Database model

PostgreSQL + pgvector. DbContext stays in Infrastructure.

### Owned tables

| Table | Purpose |
|---|---|
| `conversations` | Chat sessions keyed by external `user_id` (string) |
| `messages` | Original + normalized content, sender type |
| `faq_articles` | Approved knowledge, versioned |
| `faq_embeddings` | Vector + model + dimensions + content hash |
| `support_tickets` | Created only after user confirmation |
| `support_ticket_events` | Ticket audit trail |
| `unanswered_questions` | Knowledge-gap tracking |
| `audit_events` | FAQ/support/admin mutations |
| `ai_interaction_metadata` | Intent, retrieval, mode, latency (no CoT) |
| `idempotency_keys` | Duplicate ticket protection |
| `chat_configuration` | Prompt version / runtime flags if persisted |

### Identity contract

External Namadno user is **not** an entity. `Conversation.UserId` and `SupportTicket.UserId` are `string` (opaque identifier from trusted auth context). Same for `AssignedAgentId`.

### Indexes

- `conversations (user_id)`, `(last_message_at)`
- `messages (conversation_id)`, `(conversation_id, created_at)`
- `faq_articles (status)`, `(category)`, `(intent)`
- `support_tickets (user_id)`, `(status)`, `(assigned_agent_id)`
- `unanswered_questions (normalized_question)` unique-ish lookup
- pgvector HNSW or IVFFlat on `faq_embeddings.embedding` (cosine)

### Concurrency

Optimistic concurrency tokens on `Conversation`, `SupportTicket`, `FAQArticle`.

### Retention

Configurable. Background job deletes **only** after documented retention window. Never silent.

---

## 5. APIs (`/api/v1`)

### Chat

| Method | Path |
|---|---|
| POST | `/chat/conversations` |
| GET | `/chat/conversations` |
| GET | `/chat/conversations/{id}` |
| GET | `/chat/conversations/{id}/messages` |
| POST | `/chat/conversations/{id}/messages` |
| POST | `/chat/conversations/{id}/messages/stream` (SSE, optional) |
| POST | `/chat/conversations/{id}/support/confirm` |

### Admin FAQ

| Method | Path |
|---|---|
| GET | `/admin/faqs` |
| GET | `/admin/faqs/{id}` |
| POST | `/admin/faqs` |
| PUT | `/admin/faqs/{id}` |
| PATCH | `/admin/faqs/{id}/status` |
| DELETE | `/admin/faqs/{id}` |

### Admin support

| Method | Path |
|---|---|
| GET | `/admin/support/tickets` |
| GET | `/admin/support/tickets/{id}` |
| POST | `/admin/support/tickets/{id}/messages` |
| PATCH | `/admin/support/tickets/{id}/status` |
| PATCH | `/admin/support/tickets/{id}/assignment` |

### Admin unanswered + analytics

| Method | Path |
|---|---|
| GET | `/admin/unanswered` |
| GET | `/admin/unanswered/{id}` |
| PATCH | `/admin/unanswered/{id}/status` |
| GET | `/admin/analytics` |

### Platform

`GET /health`, `/health/live`, `/health/ready`

Response contract never exposes entities. Chat response: `conversationId`, `message`, `actions[]`, `support`. Errors: `{ error: { code, message }, traceId }`.

Stable error codes: `VALIDATION_ERROR`, `UNAUTHORIZED`, `FORBIDDEN`, `CONVERSATION_NOT_FOUND`, `CONVERSATION_ACCESS_DENIED`, `LLM_UNAVAILABLE`, `KNOWLEDGE_UNAVAILABLE`, `EXTERNAL_SERVICE_UNAVAILABLE`, `TOOL_NOT_ALLOWED`, `TOOL_EXECUTION_FAILED`, `SUPPORT_CONFIRMATION_REQUIRED`, `SUPPORT_TICKET_ALREADY_EXISTS`, `RATE_LIMITED`, `INTERNAL_ERROR`.

---

## 6. Integration boundaries

This service **expects** Namadno to provide (URLs are configuration placeholders, not invented paths):

| Port | Config key | Purpose |
|---|---|---|
| `IUserService` | `Namadno__Apis__UserProfile` | Profile read |
| `IInvestmentService` | `Namadno__Apis__InvestmentStatus` | Investment status |
| `IInvestmentService` | `Namadno__Apis__FundOrderStatus` | Fund order status |
| `IPaymentService` | `Namadno__Apis__PaymentStatus` | Payment status |
| `INeobankService` | `Namadno__Apis__TransferStatus` | Transfer status |
| `INeobankService` | `Namadno__Apis__CardStatus` | Card status |

Local default: `ExternalServices__Mode=Mock`. Mocks return deterministic data. HTTP adapters used only when mode is `Http`.

**Auth this service consumes:** JWT, API gateway identity, or trusted internal headers. `UserId` never taken from request body. See later `docs/authentication.md`.

**Auth this service requires from callers:**

- End-user APIs: authenticated user principal
- Admin APIs: permission claims (`FAQ_*`, `SUPPORT_*`, `ANALYTICS_READ`)
- Service-to-service: configured trusted identity

**Outbound:** forward `X-Correlation-Id`. Timeouts from `Namadno__TimeoutSeconds`. Retry **read-only** calls only. No financial writes exist in v1.

---

## 7. LLM architecture

Application ports:

- `IChatModel`
- `IEmbeddingModel`
- `IChatModelRouter` (primary; fallback slot reserved, not required in v1)

Infrastructure:

- `OpenAICompatibleChatModel` (LM Studio)
- `MockChatModel`
- Separate embedding client (`Embedding__BaseUrl`, `Embedding__Model`, `Embedding__Dimensions`)

Never hard-code `Qwen3.5-9B`. Chat model ≠ embedding model unless config says so.

Prompts live in `prompts/` as versioned files. Not exposed via API. Each AI response stores prompt version + model name.

Structured JSON for intent, tools, navigation, support decisions. Schema-validated. Malformed output → safe fallback, no crash.

If LLM down: deterministic FAQ / safe fallback / support offer still work.

---

## 8. RAG architecture

```
question → normalize → embed → hybrid retrieve → threshold → context → LLM or DIRECT_FAQ
```

Hybrid rank (deterministic, backend-owned):

```
score = w_vec * cosine
      + w_kw  * keyword/exact
      + w_int * intent match
      + w_cat * category match
      + w_pri * priority
```

Weights in `RagOptions`. LLM does **not** choose DB rows.

Only `FAQArticle.Status = Active` is production-retrievable.

FAQ mutate path: normalize → persist → bump version → enqueue embedding job (`IBackgroundJobDispatcher`, in-process first) → replace vector by content hash.

Configurable `Rag__UseDirectFaqFallback`: high-confidence exact match returns approved answer without paraphrase.

Response modes persisted: `DIRECT_FAQ`, `RAG_GENERATED`, `TOOL_RESULT`, `NAVIGATION`, `SUPPORT_HANDOFF`, `SAFE_FALLBACK`.

---

## 9. Security architecture

| Threat | Control |
|---|---|
| Horizontal access | Conversation/ticket scoped to `IUserContext.UserId` |
| LLM tool abuse | Allowlisted `IChatTool` + `IToolAuthorizationService`; no URL/SQL/shell tools |
| Prompt injection | User/FAQ/tool text marked as DATA; never as instructions |
| Hallucinated money/status | User-specific answers only via tools; validator rejects invented claims |
| Financial write | No write/financial tools in v1 |
| SSRF | Tool args cannot become URLs; Namadno base URLs from config only |
| Secrets | Environment/config; never logged |
| Auth spoof | Ignore client `UserId`; trusted JWT/gateway/headers only |
| Duplicate tickets | `Idempotency-Key` on support confirm |
| Abuse | Rate limits on message endpoints |
| Injection of nav | Navigation registry only (`app://investment`, etc.) |

Permissions (not hard-coded role names in domain): `FAQ_READ`, `FAQ_WRITE`, `FAQ_ACTIVATE`, `FAQ_DISABLE`, `SUPPORT_READ`, `SUPPORT_REPLY`, `SUPPORT_ASSIGN`, `SUPPORT_RESOLVE`, `ANALYTICS_READ`.

---

## 10. Testing strategy

| Suite | What |
|---|---|
| Unit | Invariants, Persian normalizer, intent schema, policy, tool authz, ownership, navigation allowlist, support confirmation |
| Integration | Testcontainers Postgres+pgvector, repositories, API, auth middleware, FAQ embedding update, support flow |
| Architecture | Domain isolation; Application must not reference Infrastructure; API is only composition root |
| Evaluation | `tests/Namadno.AI.Support.EvaluationTests/Data/persian-evaluation.json` — ≥150 cases; assert intent + behavior, not exact wording |

Acceptance scenarios 1–12 from the master spec are the release gate.

---

## 11. Deployment strategy

Local:

1. `docker compose up` → PostgreSQL + pgvector
2. LM Studio on host (not containerized)
3. `dotnet run --project src/Namadno.AI.Support.Api`

Production:

- Multi-stage Dockerfile, non-root, minimal image, HEALTHCHECK
- Config via environment (`LLM__*`, `ConnectionStrings__*`, `Namadno__*`)
- Liveness: process only
- Readiness: PostgreSQL (+ optional LLM probe, non-blocking for liveness)
- CI: restore, build, unit, architecture, integration, docker build (provider-agnostic YAML)

---

## 12. Phased delivery

| Phase | Scope | Exit |
|---|---|---|
| 0 | Repo/env inspect | SDK 10 present, empty tree |
| 1 | Solution, layers, options, architecture tests | Build + arch tests green |
| 2 | Domain + EF + migrations + seed skeleton | Migrate on empty Postgres |
| 3 | Conversation APIs + ownership | CRUD + 403 cross-user |
| 4 | Persian normalizer | Unit tests for variants |
| 5 | `IChatModel` + LM Studio adapter + mock | Config-driven model name |
| 6 | Intent classifier + thresholds | Structured + validated |
| 7 | FAQ + embeddings + pgvector | Active-only search |
| 8 | Hybrid RAG | Documented scoring |
| 9 | Response validator + safety | Advice/injection blocked |
| 10 | Tools + mock Namadno HTTP | User-specific via tools only |
| 11 | Navigation registry | No invented routes |
| 12 | Support handoff + confirm | No auto-ticket |
| 13 | Support inbox APIs | Agent reply = same conversation |
| 14 | Admin FAQ APIs + embedding job | Version + audit |
| 15 | Unanswered questions | OccurrenceCount |
| 16 | SSE streaming + fallback | Optional |
| 17 | Serilog + metrics | No full chat logs by default |
| 18 | Rate limit, headers, redaction | Hardening |
| 19 | Tests + 150 evaluation cases | Suites pass |
| 20 | Docker + CI | Image builds |
| 21 | Docs + Postman + OpenAPI + handover | Team can integrate |

After each phase: build, test, fix, re-check architecture boundaries, update docs.

---

## 13. Open points for Namadno backend team

1. Official FAQ answer corpus not in this repo — replace seed with legal/product copy before production.
2. Confirm user identifier format (`string` vs `Guid`). Implementation uses opaque `string`.
3. Finalize auth strategy (JWT vs gateway headers) and permission claim names.
4. Finalize real Namadno endpoint paths; placeholders are in config.
5. Confirm navigation deep-link scheme (`app://…`).
6. Embedding model + dimension must be chosen before first production index (dimension is a schema constraint).

---

## 14. Phase status

| Phase | Status |
|---|---|
| 0 Repo/env inspect | Done. SDK 10.0.103. Empty tree. No official FAQ corpus in repo. |
| 1 Solution + options + architecture tests | Done. |
| 2 Domain + EF + pgvector + migrations | Done. Apply with `dotnet ef database update`. |
| 3 Conversation APIs + ownership | Done. |
| 4 Persian normalizer | Done. |
| 5 LLM port + mock/hash embeddings | Done. Live LM Studio optional. |
| 6 Intent heuristic + thresholds | Done. |
| 7–8 FAQ seed + hybrid RAG | Done. Keyword-first; vector optional. |
| 9 Policy + response safety | Done. |
| 10 Tools + mock Namadno | Done. |
| 11 Navigation registry | Done. |
| 12–13 Support confirm + inbox | Done. |
| 14–15 Admin FAQ + unanswered | Done. |
| 16 SSE fallback | Done (single event). |
| 17–18 Serilog + ready + redaction + rate-limit contract | Done. |
| 19 Evaluation dataset 150 | File present; core cases asserted. |
| 20 Docker + CI | Compose stack + `.github/workflows/ci.yml`. |
| 21 Handover docs | `docs/api.md`, `authentication.md`, `namadno-integration.md`, Postman. |
