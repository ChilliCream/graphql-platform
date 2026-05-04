---
title: mock
---

The `nitro mock` commands manage mock schemas. A mock schema is a hosted GraphQL endpoint that proxies a downstream service and overlays a schema extension on top of it, useful for prototyping clients against fields that do not yet exist in the real service or for stubbing parts of an API in tests.

A mock schema is attached to an API and is defined by three pieces: the base `--schema` (the SDL of the downstream service), the `--extension` (additional types and fields layered on top), and the `--url` of the downstream service that real fields are forwarded to.

All `mock` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro mock create`

Create a new mock schema under an API.

```shell
nitro mock create \
  --api-id "<api-id>" \
  --name "<name>" \
  --schema <file-path> \
  --extension <file-path> \
  --url "<downstream-url>"
```

## Options

| Option                    | Env                           | Description                                                                        |
| ------------------------- | ----------------------------- | ---------------------------------------------------------------------------------- |
| `--api-id <api-id>`       | `NITRO_API_ID`                | ID of the API to attach the mock schema to.                                        |
| `--name <name>`           | `NITRO_MOCK_SCHEMA_NAME`      | Display name of the mock schema. Required.                                         |
| `--schema <schema>`       | `NITRO_SCHEMA_FILE`           | Path to the GraphQL file with the base schema. Required.                           |
| `--extension <extension>` | `NITRO_SCHEMA_EXTENSION_FILE` | Path to the GraphQL file with the schema extension. Required.                      |
| `--url <url>`             | `NITRO_DOWNSTREAM_URL`        | URL of the downstream GraphQL service that real fields are forwarded to. Required. |

## Examples

Create a mock schema for a downstream service:

```shell
nitro mock create \
  --api-id "<api-id>" \
  --name "<name>" \
  --schema ./schema.graphqls \
  --extension ./extension.graphql \
  --url "https://example.com/graphql"
```

# `nitro mock list`

List all mock schemas in an API. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro mock list --api-id "<api-id>"
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API. Required when running non-interactively.              |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

## Examples

List mock schemas for an API:

```shell
nitro mock list --api-id "<api-id>"
```

Page through all mock schemas in JSON mode:

```shell
nitro mock list --api-id "<api-id>" --output json
nitro mock list --api-id "<api-id>" --output json --cursor "<cursor-from-previous-page>"
```

# `nitro mock update`

Update an existing mock schema. Every option is optional, only the fields you pass are changed.

```shell
nitro mock update "<mock-schema-id>" \
  --schema <file-path> \
  --extension <file-path>
```

## Arguments

| Argument | Description                                |
| -------- | ------------------------------------------ |
| `<id>`   | ID of the mock schema to update. Required. |

## Options

| Option                    | Env                           | Description                                             |
| ------------------------- | ----------------------------- | ------------------------------------------------------- |
| `--name <name>`           | `NITRO_MOCK_SCHEMA_NAME`      | New display name of the mock schema.                    |
| `--schema <schema>`       | `NITRO_SCHEMA_FILE`           | Path to the GraphQL file with the new base schema.      |
| `--extension <extension>` | `NITRO_SCHEMA_EXTENSION_FILE` | Path to the GraphQL file with the new schema extension. |
| `--url <url>`             | `NITRO_DOWNSTREAM_URL`        | New URL of the downstream GraphQL service.              |

## Examples

Replace the schema and extension files:

```shell
nitro mock update "<mock-schema-id>" \
  --schema ./schema.graphqls \
  --extension ./extension.graphql
```

Rename a mock schema:

```shell
nitro mock update "<mock-schema-id>" --name "<name>"
```

Repoint a mock schema at a different downstream URL:

```shell
nitro mock update "<mock-schema-id>" --url "<downstream-url>"
```
