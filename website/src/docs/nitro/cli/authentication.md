---
title: Authentication
---

Nitro CLI offers two methods for authenticating your account: interactive login and API key authentication.

# Interactive Login

To initiate the interactive login process, use the `nitro login` command. This command opens a browser window where you can sign in using your preferred login provider.

```shell
nitro login
```

Once logged in, you will be asked to select your default workspace. If you don't have a default workspace yet, go to [nitro.chillicream.com](https://nitro.chillicream.com) and sign in there. Upon signing in, a workspace will automatically be created for you.

> You can quickly navigate to the Nitro site using the `nitro launch` command.

After you have selected your workspace, your setup is complete and you're ready to start using Nitro CLI.

# API Key Authentication

The second method for authentication is via API keys. API keys are unique identifiers that grant access to your workspace without the need for interactive login. They are useful for automating tasks or for use in a Continuous Integration/Continuous Deployment (CI/CD) pipeline.

You can use the `api-key` subcommand in Nitro CLI to manage your API keys. How you can manage the api keys, you can read in the [API Key Management](/docs/nitro/cli-commands/api-key) section.
Remember to keep your API keys secure, as they provide full access to your workspace. If an API key is compromised, make sure to delete it and create a new one.
