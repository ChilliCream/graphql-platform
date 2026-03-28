# Local Development

## GraphQL client generation

If you change any `*.graphql` files under `../CommandLine`, regenerate the GraphQL client:

```bash
dotnet run --framework net10.0 --project ../../../../StrawberryShake/Tooling/src/dotnet-graphql/dotnet-graphql.csproj generate
```

## Persisted operations

The deployed Nitro backend only allows persisted operations. If you changed any `*.graphql` files, regenerate persisted operations:

```bash
dotnet run --framework net10.0 --project ../../../../StrawberryShake/Tooling/src/dotnet-graphql/dotnet-graphql.csproj generate --relayFormat -q persisted
```

These operations are published during the Nitro release.

## Updating `schema.graphql`

Copy over the schema artifact from Cloud and remove sensitive directives:

```bash
perl -i -0777 -pe 's/\s*@(authorize|cost|listSize)(?:\([^)]*\)|\s*\([^)]*\n(?:.*?\n)*?\s*\))?//gs' schema.graphql
```

The replacement can leave malformed definitions of `@authorize`, `@cost`, and `@listSize`. Remove those definitions manually at the bottom of the file.
