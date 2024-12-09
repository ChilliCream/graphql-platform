# InlineFragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
    ... {
      averageRating
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
          name
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

