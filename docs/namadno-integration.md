# Namadno integration

This service is a standalone bounded context. It never writes funds, payments, transfers, cards, or insurance policies.

## Mode

| `ExternalServices:Mode` | Behavior |
|---|---|
| `Mock` (default) | Deterministic local data. No network. |
| `Http` | GET-only adapters against configured placeholders |

Switching to `Http` without real Namadno URLs will fail tools with `EXTERNAL_SERVICE_UNAVAILABLE` and the chat path falls back safely.

## Configuration placeholders

Do not invent production paths. Replace these with the real Namadno contracts:

| Port | Config |
|---|---|
| User profile | `Namadno__Apis__UserProfile` |
| Investment status | `Namadno__Apis__InvestmentStatus` |
| Fund order status | `Namadno__Apis__FundOrderStatus` |
| Payment status | `Namadno__Apis__PaymentStatus` |
| Transfer status | `Namadno__Apis__TransferStatus` |
| Card status | `Namadno__Apis__CardStatus` |
| Insurance policy | `Namadno__Apis__InsurancePolicy` |

`Namadno__BaseUrl` is the only host. Relative paths cannot become arbitrary URLs (no SSRF via tool args).

Timeout: `Namadno__TimeoutSeconds`. Retries apply to these GET calls only (standard HTTP resilience). Chat/embedding POSTs are not retried.

## Request contract (HTTP mode)

Each call:

- Method: `GET`
- Query: `userId` from **trusted** `IUserContext` (the same id the tool received)
- Headers: `X-User-Id`, `X-Correlation-Id`

## Response mapping

The adapter does **not** invent amounts, fees, or SLAs.

If the JSON body has a short safe `message` / `summary` / `statusText` / `description` string, that text is shown. URLs and prompt-injection phrases are dropped. Otherwise a conservative generic Persian sentence is used.

`404` → “no record for this user”. Other non-success → tool failure → safe fallback + support offer.

## LLM / embeddings

| Config | Purpose |
|---|---|
| `LLM__Provider=Mock` | No chat completions (FAQ/tools still work) |
| `LLM__Provider=OpenAICompatible` | LM Studio / OpenAI-compatible `POST /chat/completions` |
| `Embedding__Provider=Mock` | Hash vector (dev only) |
| `Embedding__Provider=OpenAICompatible` | `POST /embeddings` |

Chat model and embedding model are separate. Embedding dimension is a schema constraint (`768` in the initial migration). Changing it requires a new index.

LM Studio is expected on the host. From Docker use `host.docker.internal`.
