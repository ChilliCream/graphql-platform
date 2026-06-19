# GraphQL.NET to Hot Chocolate migration example

Two small, equivalent GraphQL servers built around the same Books/Authors domain.
They back the migration guide at
`website/src/docs/hotchocolate/v16/migrating/migrate-from-graphql-dotnet.md`.

| Project | Stack | Endpoint |
| --- | --- | --- |
| [`before-graphql-dotnet/`](./before-graphql-dotnet) | GraphQL.NET 8 (code-first `ObjectGraphType`) | http://localhost:5101/graphql |
| [`after-hotchocolate/`](./after-hotchocolate) | Hot Chocolate v16 (annotation-based) | http://localhost:5102/graphql |

Both expose the same schema concepts: object types with a nested list, an enum,
a union (`SearchResult`), an input object, arguments, a mutation, a subscription,
a batch DataLoader (authors for books), cursor pagination, and an authorized field.

## Running

```bash
# terminal 1
dotnet run --project before-graphql-dotnet   # http://localhost:5101

# terminal 2
dotnet run --project after-hotchocolate       # http://localhost:5102
```

Open the GraphQL IDE:

- GraphQL.NET: http://localhost:5101/ui/graphiql (GraphiQL)
- Hot Chocolate: http://localhost:5102/graphql (Nitro, built in)

## Proving equivalence

[`operations.graphql`](./operations.graphql) holds the shared operation set. The
query results (`Q1` to `Q5`, and the `P1`/`P2` node data) are identical between
the two servers. Three differences are intentional and explained in the guide:

1. **Mutation shape.** Hot Chocolate's mutation conventions wrap the field in an
   `input` argument and a `Payload` return type (`addBook(input: ...) { book { ... } }`).
2. **Authorization error shape.** The error code and structure differ.
3. **Cursor encoding.** Both paginate over the same data and return the same
   nodes, but the opaque cursor strings are encoded differently, so persisted
   cursors are not portable between the servers.

Send `X-Authenticated: true` as a request header to satisfy the `secret` field.
