---
title: "Tooling"
---

> We are still working on the documentation for Strawberry Shake, so help us by finding typos, missing things, or write some additional docs with us.

StrawberryShake comes with some tools that integrate into the dotnet CLI and help to setup a project or update the schema of a project.

# Initialize Project

`dotnet graphql init http://localhost/graphql`

The `init` command allows you to initialize a C# project for use with Strawberry Shake. It essentially creates the initial configuration file `.graphqlrc.json` and downloads the GraphQL schema.

`dotnet graphql init {url} [-p|--Path] [-n|--clientName] [--token] [--scheme] [--tokenEndpoint] [--clientId] [--clientSecret] [--scope] [-x|--headers]`

| Argument           | Description                                                                                          |
| ------------------ | ---------------------------------------------------------------------------------------------------- |
| -p or --path       | The path where the project is located.                                                               |
| -n or --clientName | The name of the client, which will also become the name of the client class in C#.                   |
| --token            | A token to interact with the server.                                                                 |
| --scheme           | The token schema that shall be used.                                                                 |
| --tokenEndpoint    | The token endpoint that shall be used to acquire a new token.                                        |
| --clientId         | The client ID that shall be used when interacting with the `tokenEndpoint`.                          |
| --clientSecret     | The client secret that shall be used when interacting with the `tokenEndpoint`.                      |
| --scope            | The scope (can be used multiple times) that shall be used when interacting with the `tokenEndpoint`. |
| -x or --headers    | The headers adds additional custom headers. Example: --headers key1=value1 --headers key2=value2     |

# Update Project

`dotnet graphql update`

The update command allows you to update the local GraphQL schema with the newest version of the GraphQL server.

`dotnet graphql update [-p|--Path] [-u|--uri] [--token] [--scheme] [--tokenEndpoint] [--clientId] [--clientSecret] [--scope] [-x|--headers]`

| Argument        | Description                                                                                          |
| --------------- | ---------------------------------------------------------------------------------------------------- |
| -p or --path    | The path where the project is located.                                                               |
| -u or --uri     | The GraphQL server URI that shall be used instead of the one in the `graphqlrc.json`                 |
| --token         | A token to interact with the server.                                                                 |
| --scheme        | The token schema that shall be used.                                                                 |
| --tokenEndpoint | The token endpoint that shall be used to acquire a new token.                                        |
| --clientId      | The client ID that shall be used when interacting with the `tokenEndpoint`.                          |
| --clientSecret  | The client secret that shall be used when interacting with the `tokenEndpoint`.                      |
| --scope         | The scope (can be used multiple times) that shall be used when interacting with the `tokenEndpoint`. |
| -x or --headers | The headers adds additional custom headers. Example: --headers key1=value1 --headers key2=value2     |

# Download Schema

`dotnet graphql download http://localhost/graphql`

The download command allows downloading a GraphQL schema from any GraphQL server.

`dotnet graphql download {url} [-f|--fileName] [--token] [--scheme] [--tokenEndpoint] [--clientId] [--clientSecret] [--scope] [-x|--headers]`

| Argument         | Description                                                                                          |
| ---------------- | ---------------------------------------------------------------------------------------------------- |
| -f or --fileName | The name of the file name. If not specified, the file will be called `Schema.graphql`.               |
| --token          | A token to interact with the server.                                                                 |
| --scheme         | The token schema that shall be used.                                                                 |
| --tokenEndpoint  | The token endpoint that shall be used to acquire a new token.                                        |
| --clientId       | The client ID that shall be used when interacting with the `tokenEndpoint`.                          |
| --clientSecret   | The client secret that shall be used when interacting with the `tokenEndpoint`.                      |
| --scope          | The scope (can be used multiple times) that shall be used when interacting with the `tokenEndpoint`. |
| -x or --headers  | The headers adds additional custom headers. Example: --headers key1=value1 --headers key2=value2     |
