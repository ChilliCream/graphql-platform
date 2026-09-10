# Create_Should_RejectInvalidKind_When_KindIsNotAnApiKind

## Exit code

```json
1
```

## Standard output

```text
Description:
  Create a new API.

Usage:
  nitro api create [options]

Options:
  --path <path>                               The path to the API [env: NITRO_API_PATH]
  --name <name>                               The name of the API [env: NITRO_API_NAME]
  --workspace-id <workspace-id>               The ID of the workspace [env: NITRO_WORKSPACE_ID]
  --kind <collection|gateway|router|service>  The kind of the API (gateway is a legacy alias for router) [env: NITRO_API_KIND]
  --cloud-url <cloud-url>                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
  --api-key <api-key>                         The API key or PAT used for authentication [env: NITRO_API_KEY]
  --output <json>                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
  -?, -h, --help                              Show help and usage information

Example:
  nitro api create --name "my-api" --kind router
```

## Standard error

```text
Argument 'source-schema' not recognized. Must be one of:
	'collection'
	'service'
	'router'
	'gateway'
```
