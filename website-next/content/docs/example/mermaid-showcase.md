---
title: Mermaid Showcase
description: Exercises the docs renderer with a variety of Mermaid diagram types.
---

A mixed bag of Mermaid diagrams used to verify the build-time SVG pipeline.
Every diagram below is rendered to inline SVG at build time, so no Mermaid
runtime ships to the browser.

# Flowchart

```mermaid
flowchart LR
  Client[GraphQL Client] -->|HTTP| Gateway
  Gateway -->|subscribes| PubSub[(PubSub)]
  Gateway -->|resolves| OrdersSubgraph
  Gateway -->|resolves| UsersSubgraph
  Gateway -->|resolves| ProductsSubgraph
  OrdersSubgraph --> OrdersDB[(Orders DB)]
  UsersSubgraph --> UsersDB[(Users DB)]
  ProductsSubgraph --> ProductsDB[(Products DB)]
  subgraph Composition
    direction TB
    Compose([fusion compose]) --> Archive[(.far archive)]
    Archive --> Gateway
  end
```

# Sequence Diagram

```mermaid
sequenceDiagram
  autonumber
  participant C as Client
  participant G as Gateway
  participant O as Orders
  participant U as Users
  participant P as Products
  C->>G: query { order(id: 1) { items { name }, customer { email } } }
  par
    G->>O: fetch order(1)
    O-->>G: { items: [...], customerId: 7 }
  and
    G->>P: fetch products(itemIds)
    P-->>G: [{ name: "Coffee" }, { name: "Mug" }]
  end
  G->>U: fetch user(7)
  U-->>G: { email: "a@b.c" }
  G-->>C: composed response
  Note over G: All hops run inside a single tracing context
```

# Class Diagram

```mermaid
classDiagram
  direction LR
  class Schema {
    +types: TypeMap
    +directives: DirectiveMap
    +query: ObjectType
    +mutation?: ObjectType
    +subscription?: ObjectType
    +validate() ValidationResult
  }
  class ObjectType {
    +name: string
    +fields: FieldMap
    +interfaces: InterfaceType[]
    +resolveType()
  }
  class FieldMap
  class TypeMap
  class DirectiveMap
  Schema "1" o-- "*" ObjectType
  ObjectType "1" o-- "1" FieldMap
  Schema "1" o-- "1" TypeMap
  Schema "1" o-- "1" DirectiveMap
  ObjectType <|-- QueryType
  ObjectType <|-- MutationType
  ObjectType <|-- SubscriptionType
```

# State Diagram

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> Resolving: query received
  Resolving --> Batching: needs dataloader
  Batching --> Resolving: data ready
  Resolving --> Streaming: @defer / @stream
  Streaming --> Resolving: next chunk
  Resolving --> Responding: result complete
  Responding --> [*]
  Resolving --> Error: resolver throws
  Error --> Responding: error in result
```

# Entity Relationship Diagram

```mermaid
erDiagram
  USER ||--o{ ORDER : places
  ORDER ||--|{ ORDER_ITEM : contains
  PRODUCT ||--o{ ORDER_ITEM : "appears in"
  USER {
    int id PK
    string email
    string name
    datetime createdAt
  }
  ORDER {
    int id PK
    int userId FK
    decimal total
    string status
  }
  ORDER_ITEM {
    int orderId FK
    int productId FK
    int quantity
    decimal unitPrice
  }
  PRODUCT {
    int id PK
    string name
    decimal price
    int stock
  }
```

# Git Graph

```mermaid
gitGraph
  commit id: "init"
  commit id: "add fusion"
  branch fusion-v2
  checkout fusion-v2
  commit id: "compose .far"
  commit id: "publish"
  checkout main
  merge fusion-v2 tag: "v16.0.0"
  branch hotfix
  commit id: "patch resolver"
  checkout main
  merge hotfix
```

# Pie Chart

```mermaid
pie showData
  title Request mix
  "queries"        : 78
  "mutations"      : 14
  "subscriptions"  :  8
```
