# Namadno AI Support API

Base URL (Docker Compose): `http://localhost:5080`

All chat and admin routes are under `/api/v1`. Health endpoints are unversioned.

Identity is never taken from the request body. In `Auth:Strategy=Development` the user is `Auth:DevelopmentUserId`, overridable with `X-Debug-User-Id`.

## Health

| Method | Path | Meaning |
|---|---|---|
| GET | `/health` | Process is up |
| GET | `/health/live` | Liveness (Docker HEALTHCHECK) |
| GET | `/health/ready` | PostgreSQL is reachable |

## Chat

| Method | Path | Notes |
|---|---|---|
| POST | `/api/v1/chat/conversations` | Creates a conversation and welcome message |
| GET | `/api/v1/chat/conversations` | Current user's conversations |
| GET | `/api/v1/chat/conversations/{id}` | 403 if another user |
| GET | `/api/v1/chat/conversations/{id}/messages` | History |
| POST | `/api/v1/chat/conversations/{id}/messages` | Rate limited (`chat`) |
| POST | `/api/v1/chat/conversations/{id}/messages/stream` | SSE, one complete JSON event |
| POST | `/api/v1/chat/conversations/{id}/support/confirm` | Header `Idempotency-Key` recommended |

### Send message body

```json
{ "content": "چطور میتونم سرمایه گذاری کنم؟" }
```

`userId` in the body is ignored if present.

### Chat response

```json
{
  "conversationId": "…",
  "message": { "id": "…", "sender": "assistant", "content": "…", "createdAt": "…" },
  "actions": [{ "type": "navigate", "label": "رفتن به سرمایه‌گذاری", "route": "app://investment" }],
  "support": { "offered": false, "reason": null },
  "mode": "DirectFaq",
  "intent": "INVESTMENT_START"
}
```

`mode` values: `DirectFaq`, `RagGenerated`, `ToolResult`, `Navigation`, `SupportHandoff`, `SafeFallback`, `Conversational` (enum names). Greetings and small-talk use `Conversational`.

`actions[].route` is only from the navigation registry (`app://investment`, `app://leasing`, `app://neobank`, `app://insurance`, `app://public-services`).

Support tickets are **not** created until `POST .../support/confirm`. Duplicate open tickets return `SUPPORT_TICKET_ALREADY_EXISTS` (409).

## Admin

Admin handlers require permission claims (`FAQ_*`, `SUPPORT_*`, `ANALYTICS_READ`). Development strategy grants all of them.

| Method | Path |
|---|---|
| GET/POST | `/api/v1/admin/faqs` |
| GET/PUT | `/api/v1/admin/faqs/{id}` |
| PATCH | `/api/v1/admin/faqs/{id}/status` |
| DELETE | `/api/v1/admin/faqs/{id}` (disables) |
| GET | `/api/v1/admin/support/tickets` |
| GET | `/api/v1/admin/support/tickets/{id}` |
| POST | `/api/v1/admin/support/tickets/{id}/messages` |
| PATCH | `/api/v1/admin/support/tickets/{id}/status` |
| PATCH | `/api/v1/admin/support/tickets/{id}/assignment` |
| GET | `/api/v1/admin/unanswered` |
| GET | `/api/v1/admin/unanswered/{id}` |
| PATCH | `/api/v1/admin/unanswered/{id}/status` |
| GET | `/api/v1/admin/analytics` |

Agent replies land as `SupportAgent` messages on the **same** conversation.

## Errors

```json
{ "error": { "code": "CONVERSATION_ACCESS_DENIED", "message": "…" }, "traceId": "…" }
```

Stable codes: `VALIDATION_ERROR`, `UNAUTHORIZED`, `FORBIDDEN`, `CONVERSATION_NOT_FOUND`, `CONVERSATION_ACCESS_DENIED`, `LLM_UNAVAILABLE`, `KNOWLEDGE_UNAVAILABLE`, `EXTERNAL_SERVICE_UNAVAILABLE`, `TOOL_NOT_ALLOWED`, `TOOL_EXECUTION_FAILED`, `SUPPORT_CONFIRMATION_REQUIRED`, `SUPPORT_TICKET_ALREADY_EXISTS`, `RATE_LIMITED`, `CONVERSATION_CLOSED`, `INTERNAL_ERROR`.

OpenAPI (Development only): `GET /openapi/v1.json`.
