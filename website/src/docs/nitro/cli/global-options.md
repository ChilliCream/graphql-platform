---
title: Global Options
---

Many commands in the `nitro` CLI share common options. These options are used to configure the CLI's behavior and authentication. Here are the common options:

# `--cloud-url <cloud-url>`

Specifies the URL where the Nitro server is located. By default, this points to the shared clusters at `api.chillicream.com`. If you have a self-hosted instance or a dedicated instance, you need to change this value to point to your server.

You can set this option from the environment variable `NITRO_CLOUD_URL`.

# `--api-key <api-key>`

Accepts API keys created via the `nitro api-key create` command or personal access tokens (PATs) created with the `nitro pat create` command. If you are running in the interactive mode in the CLI, you can also use `nitro login` to authenticate.

You can set this option from the environment variable `NITRO_API_KEY`.

# `--output <json>`

By default, the CLI uses rich text output suitable for human reading. If you want to parse the output programmatically (e.g., using `jq`), you can use this option to get the output in JSON format. This is especially useful when using the CLI in automation scripts or CI/CD pipelines.

You can set this option from the environment variable `NITRO_OUTPUT_FORMAT`.
