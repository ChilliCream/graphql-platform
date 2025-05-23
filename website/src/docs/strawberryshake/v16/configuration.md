---
title: Configuration
---

Strawberry Shake is configured by altering the `.graphqlrc.json` at the root of your project.
All settings to into `extensions.strawberryShake` object.
Here is a full configuration with all possibilities:

```json
{
  // The path of the schema file, that will be used to generate the client.
  // This setting may also be used by other tooling, because it is a default field of graphql-spec
  "schema": "schema.graphql",
  // The selector that determines, what files will be regarded as graphql documents
  // This setting may also be used by other tooling, because it is a default field of graphql-spec
  "documents": "**/*.graphql",
  "extensions": {
    // Here do only Strawberry Shake specific settings live
    "strawberryShake": {
      // The name of the generated client
      "name": "ChatClient",
      // The namespace of all the generated files of the client
      "namespace": "Demo",
      // The URL of the GraphQL api you want to consume with the client
      "url": "https://workshop.chillicream.com/graphql/",
      // The access level modifier of the generated client
      "accessModifier": "public",
      // Shall your client be based on dependency injection? If yes, all needed setup code
      // will be generated for you, so that you only have to add the client to your DI container.
      "dependencyInjection": true
    }
  }
}
```
