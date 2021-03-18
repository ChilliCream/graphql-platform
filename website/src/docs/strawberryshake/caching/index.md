---
title: "Caching"
---

StrawberryShake stores the result of GraphQL requests in a normalized entity store. The entity store allows your client to execute GraphQL requests with various strategies to reduce the need for network requests. Moreover, the normalized entities are updated by every request the client does, which means that you can build fully reactive components that change as the state in the store changes.

```mermaid
sequenceDiagram
    participant Generated Client
    participant Operation Store
    participant Entity Store
    participant GraphQL Server
    Generated Client->>Operation Store: Queries local store
    Operation Store->>GraphQL Server: Queries GraphQL server
    Note over Entity Store: Normalize response into entities
    GraphQL Server->>Entity Store: Returns GraphQL response
    Note over Operation Store: Builds operation result from entities
    Entity Store->>Operation Store: Returns entities for operation
    Operation Store->>Generated Client: Returns operation result
```
