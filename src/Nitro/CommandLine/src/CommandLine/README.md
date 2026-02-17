# Local Development

## "Secrets"

Client Ids for the Nitro Backend are inserted during the release, otherwise they're empty string.
To run commands against the Backend locally, you'll have to create a `Directory.Build.user.props` file in this directory and specify the "secrets" inserted during a local build:

```xml
<Project>
  <PropertyGroup>
    <NitroApiClientId></NitroApiClientId>
    <NitroIdentityClientId></NitroIdentityClientId>
    <NitroIdentityScopes></NitroIdentityScopes>
  </PropertyGroup>
</Project>
```

This file is git-ignored and **shouldn't be checked in**!

## Developing against local Nitro

If you change any `*.graphql` files, you need to re-generate the GraphQL client:

```bash
dotnet graphql generate
```

## Before publishing

The deployed Nitro Backend only allows persisted operations, so if you change any `*.graphql` files, you need to make sure to re-generate the persisted operations:

```bash
dotnet graphql generate --relayFormat -q persisted
```

This operations are then published to Nitro during the release.

## Updating schema.graphql

Copy over the schema artifact from the Cloud and remove all of the sensitive directives:

```bash
perl -i -0777 -pe 's/\s*@(authorize|cost|listSize)(?:\([^)]*\)|\s*\([^)]*\n(?:.*?\n)*?\s*\))?//gs' schema.graphql
```

The find and replace messes up the directive definitions of `@authorize`, `@cost`, and `@listSize`, so scroll to the bottom and remove these.
