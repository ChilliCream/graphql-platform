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
