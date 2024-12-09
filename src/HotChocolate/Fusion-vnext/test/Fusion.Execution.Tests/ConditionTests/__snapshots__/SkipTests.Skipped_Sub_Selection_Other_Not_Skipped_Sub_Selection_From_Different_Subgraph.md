# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Different_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip)
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
      query($skip: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) {
          name @skip(if: $skip)
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

