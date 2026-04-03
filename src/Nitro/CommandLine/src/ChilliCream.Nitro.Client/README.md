# Local Development

## GraphQL client generation

If you change any `*.graphql` files, regenerate the GraphQL client:

```bash
dotnet run --framework net10.0 --project ../../../../StrawberryShake/Tooling/src/dotnet-graphql/dotnet-graphql.csproj generate
```

Before merging your changes, you also need to regenerate the persisted operations.

```bash
dotnet run --framework net10.0 --project ../../../../StrawberryShake/Tooling/src/dotnet-graphql/dotnet-graphql.csproj generate --relayFormat -q persisted
```

These operations are published to the Management API during the packages release.
