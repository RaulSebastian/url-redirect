# V1 Contract

This document defines the contract that the frontend, Azure Functions, and Table Storage should all share for the first production version.

## Create Request

The create endpoint accepts a single redirect definition:

```json
{
  "alias": "summer-sale",
  "targetUrl": "https://example.com/campaign"
}
```

Rules:

- `alias` is required.
- `targetUrl` is required.
- Leading and trailing whitespace is trimmed before validation.
- The canonical alias is always lowercase.
- Reserved aliases such as `ui` and `api` are rejected.

## Alias Rules

- Length must be 3 to 40 characters after trimming.
- Allowed characters are lowercase letters `a-z`, digits `0-9`, hyphen `-`, and underscore `_`.
- The first character must be a lowercase letter or digit.
- Aliases are normalized to lowercase before persistence and comparison.
- Slashes, dots, spaces, query strings, fragments, and other symbols are not allowed.
- `ui` and `api` are reserved and cannot be created as aliases.

## Duplicate Alias Behavior

- Alias uniqueness is case-insensitive because aliases are normalized to lowercase before lookup and storage.
- Create is not an upsert.
- If the normalized alias already exists, the create operation fails with `409 Conflict`.
- The existing redirect is left unchanged.

## Create Success Response

The create endpoint should return the canonical redirect definition that was stored:

```json
{
  "alias": "summer-sale",
  "shortUrl": "https://go.example.com/summer-sale",
  "targetUrl": "https://example.com/campaign",
  "statusCode": 302
}
```

Suggested HTTP status:

- `201 Created` for a new redirect
- `409 Conflict` when the alias already exists
- `400 Bad Request` when validation fails

## Redirect Lookup Behavior

When a visitor opens `/{alias}`:

- The incoming alias is trimmed and normalized to lowercase.
- The system looks up the redirect by the normalized alias.
- If found, the system returns an HTTP redirect to `targetUrl`.
- If not found, the system returns `404 Not Found`.

## Frontend Route Behavior

- The create UI is hosted at `/ui`.
- Front Door should redirect `/` to `/ui`.
- Frontend assets are served from `/ui/assets/*`.
- Create requests are sent to `/api/redirects`.
- Root paths other than reserved UI/API paths are treated as redirect aliases.

## Storage Record Shape

V1 stores one record per redirect in Azure Table Storage with a single canonical key.

Recommended record shape:

```json
{
  "PartitionKey": "redirect",
  "RowKey": "summer-sale",
  "TargetUrl": "https://example.com/campaign",
  "CreatedUtc": "2026-04-12T10:15:30Z"
}
```

Notes:

- `PartitionKey` is fixed to `redirect` in v1 for simplicity.
- `RowKey` is the normalized alias and is the unique key.
- `TargetUrl` stores the final absolute destination URL.
- `CreatedUtc` records when the redirect was created.
- Redirect status code is intentionally not stored per record in v1 because the service uses one global redirect mode.

## Redirect Status

- All successful redirects return `302 Found`.
- The redirect status is service-wide and is not stored per record.
- `302` is the v1 choice because target URLs may change, caching is handled by our own platform layers, and browser or crawler permanence signals are not the primary concern.
