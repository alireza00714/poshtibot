# Authentication

This service does **not** own Namadno users. It consumes a trusted identity from the caller.

`UserId` is never read from JSON bodies.

## Strategies (`Auth:Strategy`)

| Value | When to use | User id source |
|---|---|---|
| `Development` | Local / Docker default | `Auth:DevelopmentUserId` or `X-Debug-User-Id` |
| `Jwt` | Bearer tokens from Namadno IdP | JWT `sub` or `NameIdentifier` |
| `Gateway` | API gateway already authenticated the user | `Auth:GatewayUserIdHeader` (default `X-User-Id`) |
| `TrustedHeaders` | Internal mesh with a shared secret | Same user header **and** `Auth:TrustedHeadersSharedSecretHeader` |

### JWT

Set:

- `Auth__Strategy=Jwt`
- `Auth__JwtAuthority` (required)
- `Auth__JwtAudience` (optional; validated when non-empty)

Permissions are JWT claims named `permission` or `permissions`. Roles are `role` / `ClaimTypes.Role`.

HTTPS metadata is required outside Development.

### Gateway

Set `Auth__Strategy=Gateway`. The gateway must send:

- `X-User-Id` (or `Auth:GatewayUserIdHeader`)
- `X-Permissions` comma-separated (`FAQ_READ`, `SUPPORT_REPLY`, …)
- `X-Roles` optional

If `Auth:TrustedHeadersSharedSecret` is non-empty, the request must also send that value in `Auth:TrustedHeadersSharedSecretHeader` (default `X-Internal-Auth`). Comparison is constant-time.

### TrustedHeaders

Same as Gateway, but the shared secret is **required**. Empty secret → 401.

## Permissions

Bind IdP roles to these names in the host. Domain code does not hard-code role names.

| Permission | Used by |
|---|---|
| `FAQ_READ` | List/get FAQ, unanswered |
| `FAQ_WRITE` | Create/update FAQ, unanswered status |
| `FAQ_ACTIVATE` | Activate FAQ |
| `FAQ_DISABLE` | Disable FAQ |
| `SUPPORT_READ` | Ticket inbox |
| `SUPPORT_REPLY` | Agent reply on the conversation |
| `SUPPORT_ASSIGN` | Assignment |
| `SUPPORT_RESOLVE` | Ticket status |
| `ANALYTICS_READ` | `/admin/analytics` |

Development grants all of the above.

## Correlation

Inbound `X-Correlation-Id` is echoed. If missing, the API generates one. Outbound Namadno HTTP calls forward the same header.
