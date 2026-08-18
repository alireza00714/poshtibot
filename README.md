# Namadno AI Support Microservice

Standalone AI customer-support backend for the Namadno Super App. No mobile/web UI. No Namadno user directory. No financial execution.

**Stack:** .NET 10, PostgreSQL + pgvector, LM Studio (OpenAI-compatible), Docker.

## Docs

| Doc | Audience |
|---|---|
| [docs/implementation-plan.md](docs/implementation-plan.md) | Architecture and phase status |
| [docs/api.md](docs/api.md) | HTTP contract |
| [docs/authentication.md](docs/authentication.md) | JWT / gateway / development identity |
| [docs/lm-studio.md](docs/lm-studio.md) | Local LM Studio server |
| [docs/namadno-integration.md](docs/namadno-integration.md) | Mock vs HTTP Namadno adapters |
| [docs/postman/Namadno.AI.Support.postman_collection.json](docs/postman/Namadno.AI.Support.postman_collection.json) | Local smoke |

## Solution

```
Namadno.AI.Support.sln
  src/Namadno.AI.Support.Domain
  src/Namadno.AI.Support.Application
  src/Namadno.AI.Support.Infrastructure
  src/Namadno.AI.Support.Api
  tests/Namadno.AI.Support.UnitTests
  tests/Namadno.AI.Support.IntegrationTests
  tests/Namadno.AI.Support.ArchitectureTests
  tests/Namadno.AI.Support.EvaluationTests
```

Dependency rule: `Api → Application → Domain`. Infrastructure implements Application ports. Domain has no EF/ASP.NET/LLM packages.

## Local run

```bash
docker compose up --build
```

API: `http://localhost:5080`  
Postgres: `localhost:5432`  
Health: `GET /health/live`, `GET /health/ready`

Default identity: `Auth__Strategy=Development`, user `dev-user` (override with `X-Debug-User-Id`).

```bash
dotnet test Namadno.AI.Support.sln
dotnet run --project src/Namadno.AI.Support.Api
```

Copy `.env.example` to `.env` and keep secrets out of git.

## Configuration

| Switch | Local default | Production |
|---|---|---|
| `LLM__Provider` | `OpenAICompatible` | LM Studio on `:1234` |
| `Embedding__Provider` | `OpenAICompatible` | `text-embedding-nomic-embed-text-v1.5` (768) |
| `ExternalServices__Mode` | `Mock` | `Http` + real `Namadno__Apis__*` paths |
| `Auth__Strategy` | `Development` | `Jwt` or `Gateway` |

Official FAQ copy is **not** in this repo. Seed answers are conservative placeholders — replace before production.

## Constraints

- Chatbot UI already exists; this service is JSON/SSE only.
- No buy / redeem / transfer / card-block execution.
- FAQ answers must not invent fees, SLAs, or policies.
- Navigation routes only from the registry (`app://investment`, …).
