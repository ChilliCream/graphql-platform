---
title: Global Options
---

# `-?, -h, --help`

Show help and usage information for the command. Use this on any subcommand to see its options, environment variables, and an example invocation.

# `--api-key <api-key>`

API key or Personal Access Token used to authenticate non-interactive CLI calls. Pass either an API key created via [`nitro api-key create`](./api-key.md) or a PAT created via [`nitro pat create`](./pat.md).

Set via the `NITRO_API_KEY` environment variable. For interactive use, prefer `nitro login` over passing this flag.

# `--output <json>`

Switches the CLI's output format. The only supported value is `json`, which renders a structured JSON document instead of the default human-readable output.

Setting `--output json` also enables non-interactive mode: prompts are disabled and any missing required input results in an error instead of an interactive question. Use this in CI, scripts, and any pipeline that needs to parse CLI output.

Set via the `NITRO_OUTPUT_FORMAT` environment variable.

# `--cloud-url <cloud-url>`

URL of the Nitro backend the CLI talks to. Only needed for self-hosted or dedicated deployments, the public ChilliCream Cloud is the default.

Set via the `NITRO_CLOUD_URL` environment variable. Defaults to `api.chillicream.com`.
