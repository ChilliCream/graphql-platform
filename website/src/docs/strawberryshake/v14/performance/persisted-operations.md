---
title: "Persisted Operations"
---

Persisted operations allow you to improve the performance of your GraphQL requests and the security of your GraphQL server.

Normally, when working with GraphQL, your client sends the **full** operation document to your GraphQL server:

```mermaid
sequenceDiagram
    participant Generated Client
    participant GraphQL Server
    Generated Client->>GraphQL Server: Request: { "query": "{ foo { bar } }" "variables": "..." }
    GraphQL Server->>Generated Client: Response: { "data": { "foo": { ... } } }
```

These operations can get quite big and it doesn't make much sense to always send the full operation document to your server. If your client is developed in close collaboration with your GraphQL server and your GraphQL endpoint isn't public, it also doesn't make sense to allow clients to send _any_ GraphQL operation document they desire.

With persisted operations you extract the GraphQL operations out of your client and export them to your server. During that process each operation is assigned a unique Id. Your client can now simply send such an Id to your server to request a specific operation. You no longer need to send the full operation document:

```mermaid
sequenceDiagram
    participant Generated Client
    participant GraphQL Server
    Generated Client->>GraphQL Server: Request: { "id": "abc" "variables": "..." }
    GraphQL Server->>Generated Client: Response: { "data": { "foo": { ... } } }
```

You can learn more about the benefits of persisted operations and how you can setup them up in your Hot Chocolate GraphQL server [here](/docs/hotchocolate/v14/performance/persisted-operations#benefits).

# Usage

To enable persisted operations, specify a `GraphQLPersistedOperationOutput` property in a MSBuild `PropertyGroup` in the `.csproj` of your Strawberry Shake application:

```xml
<PropertyGroup>
    <GraphQLPersistedOperationOutput>./persisted-operations</GraphQLPersistedOperationOutput>
</PropertyGroup>
```

The value in `GraphQLPersistedOperationOutput` should be the path to a directory relative to the project root directory. An empty value disables the feature and is also the default.

If you now re-build your application, the directory specified by `GraphQLPersistedOperationOutput` should be created and contain all of your persisted operation documents.

You can now make these persisted operation documents available to your GraphQL server, by for example uploading the directory.

During development you likely want the freedom to work with dynamic operations, so you can also [conditionally export](#conditional-export) your persisted operation documents.

## Output formats

Per default we create a file in the format of `<hash>.graphql` in the directory specified by the `GraphQLPersistedOperationFormat` property, for each GraphQL operation in your project.

Where the content of the file is the actual GraphQL operation and the `<hash>` is the computed hash of that operation, based on the selected [hashing algorithm](#hashing-algorithms).

Alternatively we offer the `relay` output format, where we create a single `operations.json` file in the output directory. It contains a JSON object, mapping a hash value to a GraphQL operation document.

```json
{
  "<hash1>": "query GetAssets { ... }",
  "<hash2>": "query GetPrices { ... }"
}
```

The hash (the key) is again calculated from the GraphQL operation (the value), based on the selected [hashing algorithm](#hashing-algorithms).

You can specify the format you'd like using the `GraphQLPersistedOperationFormat` property:

```xml
<PropertyGroup>
    <GraphQLPersistedOperationFormat>relay</GraphQLPersistedOperationFormat>
</PropertyGroup>
```

Possible values are `default` and `relay`.

## Hashing algorithms

Per default operation document hashes are calculated using the MD5 hashing algorithm, but you can also use other hashing formats:

```xml
<PropertyGroup>
    <GraphQLRequestHash>sha256</GraphQLRequestHash>
</PropertyGroup>
```

Possible values are `md5`, `sha1` and `sha256`.

## Conditional export

Since `GraphQLPersistedOperationFormat` and the other settings are MSBuild properties, you can easily apply conditions to them:

```xml
<GraphQLPersistedOperationFormat Condition="'$(Configuration)' == 'Release' ">
```

In the above example, persisted operation documents would only be exported if you are building your application in the `Release` configuration.
