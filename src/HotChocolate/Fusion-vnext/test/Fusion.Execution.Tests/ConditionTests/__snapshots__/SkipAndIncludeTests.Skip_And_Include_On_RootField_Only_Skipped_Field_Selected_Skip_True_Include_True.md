# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_True

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) @skip(if: true) @include(if: true) {
    name
  }
}
```

## Plan

```yaml
nodes:

```

