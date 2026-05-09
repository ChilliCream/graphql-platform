---
title: mcp
---

The `nitro mcp` commands manage MCP feature collections. An MCP feature collection bundles a versioned set of prompt and tool definitions that HotChocolate (Fusion) serves to MCP clients on a given stage.

A typical workflow is: `create` a collection on an API, `upload` a new version of its prompts and tools, optionally `validate` that version against a stage, then `publish` it.

> Validation runs automatically as part of `nitro mcp publish`. Use `nitro mcp validate` only when you need to gate a deploy step in a separate pipeline job.

All `mcp` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](./global-options.md)).

# `nitro mcp create`

Create a new MCP feature collection on an API.

```shell
nitro mcp create \
  --name "<name>" \
  --api-id "<api-id>"
```

## Options

| Option              | Env                                 | Description                                                                                                                                  |
| ------------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `--name <name>`     | `NITRO_MCP_FEATURE_COLLECTION_NAME` | Display name of the MCP feature collection. Required.                                                                                        |
| `--api-id <api-id>` | `NITRO_API_ID`                      | ID of the API the collection belongs to. Required when no workspace is set in the session. Get the ID from `nitro api list` or the Nitro UI. |

## Examples

Create a collection interactively (prompts for missing values):

```shell
nitro mcp create
```

Create a collection non-interactively:

```shell
nitro mcp create --name "<name>" --api-id "<api-id>"
```

# `nitro mcp upload`

Upload a new version of an MCP feature collection. Prompt and tool definition files are picked up via glob patterns.

```shell
nitro mcp upload \
  --mcp-feature-collection-id "<collection-id>" \
  --tag "<tag>" \
  --prompt-pattern "<prompt-pattern>" \
  --tool-pattern "<tool-pattern>"
```

## Options

| Option                                                    | Env                               | Description                                                                                |
| --------------------------------------------------------- | --------------------------------- | ------------------------------------------------------------------------------------------ |
| `--mcp-feature-collection-id <mcp-feature-collection-id>` | `NITRO_MCP_FEATURE_COLLECTION_ID` | ID of the MCP feature collection. Required.                                                |
| `--tag <tag>`                                             | `NITRO_TAG`                       | Tag of the version being uploaded (for example a Git commit SHA or release tag). Required. |
| `-p, --prompt-pattern <prompt-pattern>`                   |                                   | One or more glob patterns for locating MCP prompt definition files (`*.json`).             |
| `-t, --tool-pattern <tool-pattern>`                       |                                   | One or more glob patterns for locating MCP tool definition files (`*.graphql`).            |

## Examples

Upload prompts and tools from the default folders:

```shell
nitro mcp upload \
  --mcp-feature-collection-id "<collection-id>" \
  --tag "v1" \
  --prompt-pattern "./prompts/**/*.json" \
  --tool-pattern "./tools/**/*.graphql"
```

# `nitro mcp publish`

Publish a previously uploaded MCP feature collection version to a stage. The version is identified by its tag.

```shell
nitro mcp publish \
  --mcp-feature-collection-id "<collection-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                                                    | Env                               | Description                                                                                                                               |
| --------------------------------------------------------- | --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `--mcp-feature-collection-id <mcp-feature-collection-id>` | `NITRO_MCP_FEATURE_COLLECTION_ID` | ID of the MCP feature collection. Required.                                                                                               |
| `--tag <tag>`                                             | `NITRO_TAG`                       | Tag of the version to publish. Required.                                                                                                  |
| `--stage <stage>`                                         | `NITRO_STAGE`                     | Name of the stage to publish to. Required.                                                                                                |
| `--force`                                                 |                                   | Skip confirmation prompts for deletes and overwrites. Mutually exclusive with `--wait-for-approval`.                                      |
| `--wait-for-approval`                                     | `NITRO_WAIT_FOR_APPROVAL`         | Block the command until a reviewer approves the deployment. Mutually exclusive with `--force`. Required when the stage gates deployments. |

## Examples

Publish to `dev`:

```shell
nitro mcp publish \
  --mcp-feature-collection-id "<collection-id>" \
  --stage "dev" \
  --tag "v1"
```

Publish to a gated stage and wait for approval:

```shell
nitro mcp publish \
  --mcp-feature-collection-id "<collection-id>" \
  --stage "production" \
  --tag "v1" \
  --wait-for-approval
```

# `nitro mcp validate`

Validate a new MCP feature collection version against a stage without publishing it. Run this in your pull request validation workflow to catch breaking changes before they are merged.

```shell
nitro mcp validate \
  --mcp-feature-collection-id "<collection-id>" \
  --stage "<stage>" \
  --prompt-pattern "<prompt-pattern>" \
  --tool-pattern "<tool-pattern>"
```

## Options

| Option                                                    | Env                               | Description                                                                     |
| --------------------------------------------------------- | --------------------------------- | ------------------------------------------------------------------------------- |
| `--mcp-feature-collection-id <mcp-feature-collection-id>` | `NITRO_MCP_FEATURE_COLLECTION_ID` | ID of the MCP feature collection. Required.                                     |
| `--stage <stage>`                                         | `NITRO_STAGE`                     | Name of the stage to validate against. Required.                                |
| `-p, --prompt-pattern <prompt-pattern>`                   |                                   | One or more glob patterns for locating MCP prompt definition files (`*.json`).  |
| `-t, --tool-pattern <tool-pattern>`                       |                                   | One or more glob patterns for locating MCP tool definition files (`*.graphql`). |

## Examples

Validate against the `dev` stage:

```shell
nitro mcp validate \
  --mcp-feature-collection-id "<collection-id>" \
  --stage "dev" \
  --prompt-pattern "./prompts/**/*.json" \
  --tool-pattern "./tools/**/*.graphql"
```

# `nitro mcp list`

List all MCP feature collections of an API. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro mcp list --api-id "<api-id>"
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API. Falls back to interactive selection when omitted.     |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

# `nitro mcp delete`

Delete an MCP feature collection by its ID. Once deleted, the collection and all its versions are no longer accessible to MCP clients.

```shell
nitro mcp delete "<mcp-feature-collection-id>"
```

## Arguments

| Argument | Description                                           |
| -------- | ----------------------------------------------------- |
| `<id>`   | ID of the MCP feature collection to delete. Required. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |
