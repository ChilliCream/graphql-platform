# Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph_If_True

## Request

```graphql
query($id: ID!) {
  reviewById(id: $id) {
    body
    author @skip(if: true) {
      displayName
    }
  }
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "REVIEWS"
    operation: >-
      query($id: ID!) {
        reviewById(id: $id) {
          body
        }
      }

```

