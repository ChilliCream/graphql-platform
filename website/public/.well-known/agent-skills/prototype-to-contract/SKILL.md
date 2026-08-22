---
name: prototype-to-contract
description: >-
  Convert a local-only prototype into Relay-backed component fragments and a
  schemaExtensions backend contract. Use when mock data and local action hooks
  must become route queries, colocated fragments, and read/write GraphQL specs.
  Trigger on "prototype to contract", "mock to contract", "turn this prototype
  into a contract", "fragmentize this page", "wire this mocked page to
  GraphQL", or "spec this feature for the backend". Do not use for isolated
  fragment cleanup or schema design without a prototype UI.
---

# Prototype to contract

Replace prototype data and action seams with fragments, route queries, and backend contract SDL.

## Read

- Read the current schema, `schemaExtensions`, GraphQL Client config, route entry, prototype data, and local action hooks.
- Follow the app's existing graphql loading and generated-artifact conventions.
- Use graphql-schema-design skill for schema names, nullability, connections, mutation payloads, and typed errors.

## Components

- Preserve prototype boundaries unless the component reads fields from more than one entity.
- Give each data-bound component one primary entity and one colocated fragment for the subset it reads.
  - use the graphql clients equivalent of `useFragment` and use data masking!
- Pass related entities to child components as fragment data; do not read their fields in the parent.
- Split shells from data-bound content when the shell would otherwise own multiple entity fragments.
- Keep local UI state, formatting, icons, labels, and styling out of GraphQL.

## Reads

- Replace each rendered mock field with a schema field or schema extension field.
- Do not carry unused mock fields into GraphQL.
- Treat computed product facts as schema fields when the backend owns the truth.
- Keep pure presentation derivations in the client.
- Use connections for lists that can grow beyond a bounded UI fixture; put counts on the connection.
  - Put relationship-specific data on the connection edge, such as `joinedAt` on `UserGroupEdge`.
  - Use edge fields only for facts about that parent-to-node relationship.
  - If the UI renders the relationship as the primary object, model it as its own node and connection, such as `UserGroupMembershipConnection`, instead of overloading `UserGroupConnection`.
- Follow the naming rules of the project.

## Actions

- Start from the prototype's local mutation-shaped hooks.
- If the mutation exists in the schema, replace the hook implementation with Relay while keeping its call shape.
- Add a schema mutation for every user action in the requested workflow.

## Contract

- Put new fields, types, subscriptions, and mutations directly into the schema file.
- Docstring every added schema element (fields, types, mutations, etc.) with a precise use case description, product meaning, domain knowledge, and null/error semantics.
- Give each mutation unique `Input` and `Payload` types.
- Return a nullable changed entity plus `errors: [<Mutation>Error!]!`.
- Define the shared `Error` interface, a per-mutation error union, and concrete error types with useful fields.

## Example

```tsx
function ReviewCard({ review }: { $ref: ReviewCard_review$key }) {
  const data = useFragment(
    graphql`
      fragment ReviewCard_review on Review {
        body
        author {
          ...UserChip_user
        }
      }
    `,
    $ref,
  );

  return (
    <article>
      <p>{data.body}</p>
      <UserChip user={data.author} />
    </article>
  );
}
```

```tsx
function ReviewForm() {
  const [createReview, { loading, error }] = useCreateReviewMutation();
  return (
    <button disabled={loading} onClick={() => createReview({ body: "..." })}>
      Post
    </button>
  );
}
```

## Finish

- Wire the route query to spread the top-level feature fragments.
- Run the repo's Relay compiler and typecheck/build commands.
- Remove prototype mock data and local fake persistence.
- Review the extension file with `/graphql-schema-design review`.
