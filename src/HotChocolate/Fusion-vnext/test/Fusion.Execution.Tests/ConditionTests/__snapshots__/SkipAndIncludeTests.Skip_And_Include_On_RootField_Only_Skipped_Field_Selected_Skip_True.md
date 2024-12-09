# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) @include(if: $include) @skip(if: true) {
    name
  }
}
```

## Plan

```yaml
nodes:

```

