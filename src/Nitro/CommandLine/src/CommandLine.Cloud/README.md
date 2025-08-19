# Local Development

## "Secrets"

Client Ids for the Nitro Backend are inserted during the release, otherwise they're empty string.
To run commands against the Backend locally, you'll have to create a `Directory.Build.props.user` file in this directory and specify the "secrets" inserted during a local build:

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

## Persisted operations

The Nitro Backend only allows persisted operations, so if you change any `*.graphql` files, you need to make sure to re-generate the persisted operations:

```bash
dotnet graphql generate --relayFormat --q persisted
```

This operations are then published to Nitro during the release.
