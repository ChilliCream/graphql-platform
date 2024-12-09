# Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    averageRating @skip(if: $skip)
    reviews(first: 10) {
      nodes {
        body
      }
    }
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
          id
        }
      }
  - id: 2
    schema: "REVIEWS"
    operation: >-
      query($__fusion_requirement_1: ID!, $skip: Boolean!) {
        productById(id: $__fusion_requirement_1) {
          averageRating @skip(if: $skip)
          reviews(first: 10) {
            nodes {
              body
            }
          }
        }
      }
    requirements:
      - name: "__fusion_requirement_1"
        dependsOn: "1"
        selectionSet: "productBySlug"
        field: "id"
        type: "ID!"

```

