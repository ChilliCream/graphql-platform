---
title: Apollo Federation Subgraph Support
---

Hot Chocolate includes an implementation of the Apollo Federation v2 specification for creating Apollo Federated subgraphs. Through Apollo Federation, you can combine multiple GraphQL APIs into a single API for your consumers.

[Apollo Federation documentation](https://www.apollographql.com/docs/federation/) provides a robust explanation of the different foundational concepts and principles that are referenced through the rest of this document.

## Get Started
To use the Apollo Federation tools, you need to first install v12.6 or later of the `HotChocolate.ApolloFederation` package.

<PackageInstallation packageName="HotChocolate.ApolloFederation"/>

After installing the necessary package, you'll need to register the Apollo Federation services with the GraphQL server.

```csharp
IServiceCollection services;

services.AddGraphQLServer()
    .AddApolloFederation();
```

## Defining an entity
We'll first need to define an **entity**&mdash;an object type that can resolve its fields across multiple subgraphs&mdash;within the subgraph. To do this,
