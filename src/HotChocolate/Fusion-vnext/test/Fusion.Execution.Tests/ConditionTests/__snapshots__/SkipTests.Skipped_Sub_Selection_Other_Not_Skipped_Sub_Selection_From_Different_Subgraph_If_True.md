# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Different_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true)
    averageRating
  }
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      query($slug: String!) {
        productBySlug(slug: $slug) {
          __typename
          id
        }
      }
  - id: 2
    schema: "REVIEWS"
    operation: >-
      query($__fusion_requirement_1: ID!) {
        productById(id: $__fusion_requirement_1) {
          averageRating
        }
      }
    requirements:
      - name: "__fusion_requirement_1"
        dependsOn: "1"
        selectionSet: "productBySlug"
        field: "id"
        type: "ID!"

```

