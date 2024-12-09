# Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph

## Request

```graphql
query($id: ID!, $skip: Boolean!) {
  reviewById(id: $id) {
    body
    author @skip(if: $skip) {
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
      query($id: ID!, $skip: Boolean!) {
        reviewById(id: $id) {
          body
          author @skip(if: $skip) {
            id
          }
        }
      }
  - id: 2
    schema: "ACCOUNTS"
    operation: >-
      query($__fusion_requirement_1: ID!) {
        userById(id: $__fusion_requirement_1) {
          displayName
        }
      }
    requirements:
      - name: "__fusion_requirement_1"
        dependsOn: "1"
        selectionSet: "reviewById.author"
        field: "id"
        type: "ID!"

```

