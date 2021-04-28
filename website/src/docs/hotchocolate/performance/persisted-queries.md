---
title: "Persisted queries"
---

This guide will walk you through how standard persisted queries works and how you can set them up with the Hot Chocolate GraphQL server.

Persisted queries allows you to pre-register all required queries/mutations from your client on your GraphQl server.

This can be done by extracting the queries from your client application at build time and putting them to server query storage. Persisted client output contains queries / mutations that your client application needs to run. Each extracted query has a specific Hash (identifier) and the client uses it with Variables to query data from your server. There is no more query string in the body of your request. The server obtains only concrete identifier + variables and searches for them in AllowedList. If there is a match, the request is directly executed without the need for query parsing else the particular error message is returned to the request initiator. 

## Benefits

There are two main reasons why to use persisted queries:

- Performance - Payload contains only Hash and variables. This will reduce the size of your client application since queries can be removed from the client code at the build time.

- Security - By using persisted queries, the server will accept only known queries / mutations and refuse all others that are not part of persisted "Allowed-List". Useful mainly for public APIs. 

## Limitations

If your API is used by multiple consumers out of your environment with custom requirements for query / mutation, then this is not for your use case and you may have a look at [automatic persisted queries](automatic-persisted-queries), which probably fits better your scenario. 

## How they are stored

Persisted queries are stored close to your server either in the file system or in a Redis cache this helps to reduce request sizes.

[HC provides various places to store this queries](https://www.nuget.org/packages?q=Hotchocolate.PersistedQueries) :

Nuget namespaces:
- HotChocolate.PersistedQueries.Redis 
- HotChocolate.PersistedQueries.FileSystem  
- HotChocolate.PersistedQueries.InMemory

# Setup

To Configure GraphQL server to use the persisted query pipeline.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline();
}
```

## Configure persisted queries on server (FileStorage)

If we want to configure our GraphQL server to be able to handle persisted query requests. For this, we need to register the corresponding query storage and configure the persisted query request pipeline.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddReadOnlyFileSystemQueryStorage("./persisted_queries");
}
```

By this HC will search for correspondent query Hash in folder `./persisted_queries`. It is important to store all exported clients persisted  queries in this folder using a specific name and format: `{Hash}.graphql`

Example: `0c95d31ca29272475bf837f944f4e513.graphql`

Now your server knows where to search for the requested query `Id` and loads it in memory in case of a successful match. 


## Configure persisted  queries on the client (Relay)

This step shows what you need to configure on the client-side or adjust on the server regarding clients. Example show Relay implementation. You can use any other GraphQL client like Apollo. The configuration is always client-related so follow offical client Docs. 

[Docs Relay](https://relay.dev/docs/guides/persisted-queries/)

[Docs Apollo](https://www.apollographql.com/docs/apollo-server/performance/apq/)

### What can be different between clients

- Hashing algorithm - Hot Chocolate server is configured to use by default the MD5 hashing algorithm. HotChocolate server comes out of the box with support for MD5, SHA1, and SHA256

- Default Headers and Setup - HC expect the persisted `Id` to be named as` id` in the request header. Some docs like Relay show you to send it under `doc_id` this would not work with HC. You must adjust the client fetch function to use the correct variable. 

### Hashing algorithm setup

If adjustmed of algorithm is required you can do it using following sintax:

 > **ℹ️**  For relay it is not needed. Relay use default MD5 Hash.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // Global Services
        .AddRouting()
        .AddMemoryCache()
        .AddSha256DocumentHashProvider(HashFormat.Hex)
        
        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline()
        .AddReadOnlyFileSystemQueryStorage("./persisted_queries");
}
```

### Relay persisted queries setup

#### Enable persisted output (Frontent project part)

Make shure you have installed all packages related to Relay and [compiler](https://www.npmjs.com/package/relay-compiler). This example counts that you are allready user of Relay and will no explain deep how to Setup-it.

Extend your relay script in `package.json` by `--persist-output ./path/to/persisted-queries.json"`. Set correct path of your output.

```json
"scripts": {
  "relay": "relay-compiler --src ./src --schema ./schema.graphql --persist-output ./path/to/persisted-queries.json"
}
```

This will remove query string from generated files and use Id.

**Before:**
```json
{
  "kind": "Request",
  "operationKind": "query",
  "name": "HotchocolateTestQuery",
  "id": null,
  "text": "query HotchocolateTestQuery(\n  $itemID: ID!\n) {\n  node(id: $itemID) {\n    ...TestItem_item_2FOrhs\n  }\n}\n\nfragment TestItem_item_2FOrhs on Todo {\n    text\n    isComplete\n}\n",
}
```

**After:**
```json
{
 "kind": "Request",
  "operationKind": "query",
  "name": "TodoItemRefetchQuery",
  "id": "3be4abb81fa595e25eb725b2c6a87508", // Our new Hash
  "text": null, 
}
```

### Setup fetch function
```js
return fetch('/graphql', {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      id: operation.id, // Use Id variable!
      variables,
      // query: operation.text  !This must be null  (commented -out)
    })
```

### Generate persisted  output

```bash
yarn run relay
npm start relay
```

The Relay output is `persisted-queries.json` All-In-One-File.

Example relay output
```json
{
"b342480227f3ce0ec9e120e6147dd4fa": "query UserProviderQuery {\n  ...UserProviderMe_Fragment\n  me {\n    id\n    systemid\n  etc... }\n},
"991ed3080b704c933eb09b011ef03998": "mutation ProjectMilestonesSetStatusMutation {\n   $request: SetMailstoneS etc... },
},
```

> ⚠️ **Note:** HotChocolate server **requires** all persisted queries to be stored in the configured directory as separate files named by Hash (id) and ending as `.graphql`. Example: `0c95d31ca29272475bf837f944f4e513.graphql`. To fit Relay output and HC you need to convert `persisted-queries.json`. **You can write a custom converter for that.**

[You can use one awailable under Examples.](https://github.com/ChilliCream/hotchocolate-examples/blob/master/misc/Persisted-queries/relay-persisted-converter.js)

Put it after relay generation to convert the generated output to HC format. It is `node.js` script.

```json
"scripts": {
"relay": "relay-compiler --persist-output persisted-queries.json && node relay-presisted-converter.js persisted-output/persisted-queries.json persisted-output",
}
```

Run new generation and you are ready to go with persisted-queries.

```bash
yarn run relay
npm start relay
```

The script require source path to `persisted-queries.json` and output directory where the files will be generated. For more info read the header of the script.
