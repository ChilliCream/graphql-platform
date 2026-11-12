# Transform_Should_RewriteToChoiceOfPaths_When_ContextSelectsPerTypeCondition

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@external", "@context", "@fromContext"]) {
  query: Query
}

scalar FieldSet
scalar ContextFieldValue

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @external on FIELD_DEFINITION | OBJECT
directive @context(name: String!) repeatable on INTERFACE | OBJECT | UNION
directive @fromContext(field: ContextFieldValue) on ARGUMENT_DEFINITION
directive @link(url: String! import: [String!]) repeatable on SCHEMA
type Query {
  product: Product
  a: A
  b: B
  c: C
}

type A @context(name: "context1") {
  someField: String
}

type B @context(name: "context1") {
  someField: String
}

type C @context(name: "context1") {
  someOtherField: String
}

type Product {
  id: ID!
  label(value: String @fromContext(field: "$context1 ... on A { someField } ... on B { someField } ... on C { someOtherField }")): String
}
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  a: A
  b: B
  c: C
  product: Product
}

type A @context(name: "context1") {
  someField: String
}

type B @context(name: "context1") {
  someField: String
}

type C @context(name: "context1") {
  someOtherField: String
}

type Product {
  id: ID!
  label(
    value: String @require(field: "$context1<A>.someField | $context1<B>.someField | $context1<C>.someOtherField")
  ): String
}
```
