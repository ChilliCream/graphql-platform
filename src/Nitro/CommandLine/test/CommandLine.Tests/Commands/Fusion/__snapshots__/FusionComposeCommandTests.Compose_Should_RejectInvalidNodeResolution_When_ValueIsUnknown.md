# Compose_Should_RejectInvalidNodeResolution_When_ValueIsUnknown

## Exit code

```json
1
```

## Standard output

```text
Description:
  Compose multiple source schemas into a single composite schema.

Usage:
  nitro fusion compose [options]

Options:
  -f, --source-schema-file <source-schema-file>                               One or more paths to a source schema file (.graphqls) or directory containing a source schema file
  --source-schema-url <source-schema-url>                                     A URL from which to download a source schema
  --source-schema-settings-file <source-schema-settings-file>                 A settings file paired by occurrence with '--source-schema-url'
  -a, --archive, --configuration <archive>                                    The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
  -e, --env, --environment <environment>                                      The name of the environment used for value substitution in the schema-settings.json files
  --cache-control-merge-behavior <ignore|include|include-private>             Choose how @cacheControl directives are merged
  --enable-global-object-identification                                       Add the 'Query.node' field for global object identification
  --node-resolution <gateway|router|source-schema>                            Choose whether Query.node identifiers are resolved by the router or a source schema (gateway is a legacy alias for router)
  --tag-merge-behavior <ignore|include|include-private>                       Choose how @tag directives are merged
  --shareable-field-runtime-type-routing <common-runtime-types|source-local>  Choose how runtime types are routed for Apollo Federation shareable abstract fields
  --allow-non-resolvable-interface-objects                                    Allow Apollo Federation interface objects without a resolvable key
  --include-satisfiability-paths                                              Include paths in satisfiability error messages
  --watch                                                                     Watch for file changes and recompose automatically
  -w, --working-directory <working-directory>                                 Set the working directory for the command
  --exclude-by-tag <exclude-by-tag>                                           One or more tags to exclude from the composition
  --remove-source-schema <remove-source-schema>                               One or more source schemas to remove from the archive before composing.
  --cloud-url <cloud-url>                                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
  --api-key <api-key>                                                         The API key or PAT used for authentication [env: NITRO_API_KEY]
  --output <json>                                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
  -?, -h, --help                                                              Show help and usage information

Example:
  nitro fusion compose \
    --source-schema-file ./products/schema.graphqls \
    --source-schema-url https://reviews.example.com/graphql \
    --source-schema-settings-file ./reviews/schema-settings.json \
    --archive ./gateway.far \
    --env "dev"
```

## Standard error

```text
Argument 'invalid-value' not recognized. Must be one of:
	'router'
	'gateway'
	'source-schema'
```
