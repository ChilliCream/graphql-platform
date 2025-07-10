---
title: "Introduction to Entities"
---

In Fusion, entities are a fundamental concept that enable multiple services to collaboratively define and resolve fields for shared object types. This guide explains how entities work in Fusion and how you can use them to build a unified GraphQL API from multiple services.

# Entity Overview

In a federated schema managed by Fusion, an entity is an object type who is identified by a unique key and therefore whose fields can be resolved across multiple services.
Each service can contribute different fields to the entity and is responsible for resolving only the fields it defines.
This approach adheres to the separation of concerns principle, allowing teams to work independently while contributing to a cohesive API.

For example, consider a `Product` entity whose fields are defined and resolved across two services:

- **Products:** Defines core product information like `id`, `name`, and `price`.
- **Reviews:** Adds fields like `rating` and `reviews` to the `Product` entity.

# Defining Entities in Fusion

## Implicitly Shareable Types

In Fusion, all object types are sharable by default. This means you do not need to annotate types or fields to make them available across services. Any type defined in one service can be referenced and extended in another service without additional directives or annotations.

Field overlap between services is resolved automatically by the Fusion gateway and can be used to optimize data fetching and reduce network round trips.

## Implicit Keys via Lookups

Unlike other federated systems, Fusion does not require you to explicitly define keys for your entities. Instead, keys are defined implicitly through lookups. A lookup is any field or operation that can uniquely identify an instance of a type based on certain arguments.

For example, if a service defines a field `productById(id: ID!): Product`, Fusion understands that `Product` instances can be identified by the `id` field. If another field `productBySku(sku: String!): Product` exists, Fusion knows that `Product` can also be identified by the `sku` field. These lookups inform the gateway how to fetch and resolve entities across services.

## Using Entities Across Services

When building a federated schema with Fusion, entities allow different services to collaborate on shared types. Here's how you can use entities in your schema:

1. **Define the Entity in One Service**

   In the product service, define the entity with its core fields:

   ```graphql
   # Products
   type Product {
     id: ID!
     name: String!
     description: String
     price: Float!
   }

   type Query {
     productById(id: ID!): Product
   }
   ```

2. **Reference and Extend the Entity in Another Service**

   In another service, reference the entity and add additional fields:

   ```graphql
   # Reviews Service
   type Product {
     id: ID!
     # Additional fields can be added here
     rating: Float
     reviews: [Review!]!
   }

   type Review {
     id: ID!
     content: String!
     author: User!
   }

   type Query {
     productById(id: ID!): Product @lookup
   }
   ```

   In this example, the `Reviews` service references the `Product` type and adds fields like `rating` and `reviews`.

3. **Use Lookups to Resolve Entities**

   Fusion uses lookups defined in the services to resolve entities across services. When a client queries for fields that span multiple services, the gateway orchestrates the request using the available lookups.

## Querying Across Services

Clients can now query for product information and reviews in a single request:

```graphql
query {
  productById(id: "123") {
    id
    name
    price
    rating
    reviews {
      content
      author {
        name
      }
    }
  }
}
```

The Fusion gateway handles the orchestration between subgraphs, resolving the `Product` entity across both services using the `id` field.
