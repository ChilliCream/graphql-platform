# Two_Fragments_On_Sub_Selection_With_Different_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment1
    ... ProductFragment2
  }
}

fragment ProductFragment1 on Product {
  name
}

fragment ProductFragment2 on Product {
  description
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
          description
        }
      }

```

