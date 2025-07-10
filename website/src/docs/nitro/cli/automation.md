---
title: Automation
---

When integrating the `nitro` CLI into CI/CD pipelines or automation scripts, you can use API keys or personal access tokens (PATs) for authentication.

- **API Keys**: Ideal for use in CI/CD or for telemetry reporting in your GraphQL server. You can create API keys with the `nitro api-key create` command.

- **Personal Access Tokens (PATs)**: If you need to automate other tasks or require broader access, you can use personal access tokens. Create them with the `nitro pat create` command. Use the PAT in the CLI with the `--api-key` option.

# Passing Inputs Non-Interactively

All inputs in the CLI can be passed via environment variables or as options in the CLI commands. This allows you to skip the interactive mode and use the CLI non-interactively in your automation scripts.

For example:

- Use environment variables like `NITRO_API_ID`, `NITRO_STAGE`, etc., to provide inputs.
- Use command-line options like `--api-id`, `--stage`, etc., to specify inputs directly.

# Parsing CLI Output

By default, the CLI outputs rich text suitable for human reading. When using the CLI in automation scripts, you may want to parse the output programmatically.

- Use the `--output json` option to get the output in JSON format. This allows you to easily parse the output using tools like `jq`.

Example:

```shell
nitro api-key list --output json | jq '.'
```
