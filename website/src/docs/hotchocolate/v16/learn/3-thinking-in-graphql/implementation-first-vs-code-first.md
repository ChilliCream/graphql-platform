---
title: "Implementation-first vs code-first"
description: "Choose the Hot Chocolate v16 schema authoring style that fits your .NET project, team workflow, and schema contract."
---

# Implementation-first vs Code-first

When starting a new Hot Chocolate project, most teams should begin with **implementation-first** schema authoring. This approach keeps your GraphQL schema close to your C# resolver methods and models. However, there are cases where **code-first** is a better fit, such as when you need fine-grained control over the schema, when the GraphQL shape must differ from your CLR types, or when you cannot modify the runtime type with attributes.

Both implementation-first and code-first styles produce a GraphQL schema that clients interact with in the same way. The difference lies in how you define the schema on the server, not in what GraphQL can represent.

| Choose | When |
| --- | --- |
| Implementation-first | Building a typical .NET application schema where the GraphQL shape closely follows your resolver methods, records, DTOs, or domain models. |
| Code-first | You need explicit descriptor configuration, do not own the CLR type, or want a library to manage reusable schema rules. |

## What changes with each style?

The GraphQL schema is the contract between your server and its clients. The [GraphQL specification](https://spec.graphql.org/October2021/#sec-Type-System) describes schemas in terms of types, fields, arguments, directives, and root operation types. Hot Chocolate provides multiple C# authoring styles to help you produce this contract.

- **Implementation-first**: Maps attributed C# types, resolver methods, public members, nullability annotations, and attributes into a schema. You typically register these with `AddTypes()`.
- **Code-first**: Uses descriptor types like `ObjectType<T>` to configure schema types and fields explicitly. These are registered with methods such as `AddQueryType<T>()`, `AddType<T>()`, or `AddTypeExtension<T>()`.

When the output matches, the client sees the same schema shape:

```graphql
type Query {
  productById(id: Int!): Product
}

type Product {
  id: Int!
  name: String!
}
```

| Concern | Implementation-first | Code-first |
| --- | --- | --- |
| Authoring model | C# resolver method and CLR type are the starting point. | Descriptor classes are the starting point. |
| Field configuration | Inferred from members, nullability, and attributes, with targeted customization. | Configured through descriptor APIs. |
| Registration | Source-generated registrations are added through `AddTypes()`. | Type classes are registered explicitly. |
| Review target | Inspect the generated schema to confirm the client-visible contract. | Inspect the generated schema to confirm descriptor output. |

To compare the output from either style, use [Nitro's schema definition view](/docs/nitro/documents/schema-definition/) or [schema reference view](/docs/nitro/documents/schema-reference/). The generated schema is the contract that clients discover and depend on.

## When to use implementation-first

Implementation-first is ideal when building a .NET API from resolver methods, services, EF Core models, records, DTOs, and domain-facing types.

You add a resolver, run the server, inspect the schema, and query it. The tutorial and quick start guides use this approach because it keeps schema work close to the code that returns data.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProductById(int id)
        => id == 1 ? new Product(1, "Trail Shoe") : null;
}

public sealed record Product(int Id, string Name);
```

With the source generator and `builder.AddGraphQL().AddTypes()`, Hot Chocolate adds a `productById` field to the root `Query` type and maps `Product` to an object type.

Implementation-first works well when:
- The schema closely follows your API-facing C# model.
- Resolver method parameters naturally become GraphQL arguments or injected services.
- Nullable reference types describe most of your GraphQL nullability.
- Attributes are sufficient for naming, documentation, authorization, or field behavior.
- Contributors should find resolver code and schema intent in the same feature area.

Schema review remains important. While implementation-first removes repetitive descriptor code, you still need to choose stable names, clear nullability, descriptions, deprecations, and client-oriented field shapes. For more on how Hot Chocolate maps C# into the schema, see [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [Queries](/docs/hotchocolate/v16/building-a-schema/queries/), and [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/).

## When to use code-first

Choose code-first when descriptor APIs express schema rules more clearly than attributes or inferred member configuration.

```csharp
// Types/Product.cs
public sealed record Product(int Id, string Name, string InternalSku);

// Types/ProductType.cs
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("CatalogProduct");

        descriptor
            .Field(p => p.Id)
            .Type<NonNullType<IdType>>();

        descriptor.Ignore(p => p.InternalSku);
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddType<ProductType>();
```

Code-first is a good fit when:
- The public GraphQL type name, field names, or field set must differ from the CLR type.
- The CLR type comes from a third-party package, generated code, or a shared library.
- You want schema rules in an infrastructure package rather than attributes on application models.
- Descriptor APIs make the desired type, argument, middleware, or resolver configuration clearer to review.
- Your team prefers explicit registration for the types a module contributes.

Code-first is not a fallback. It trades convention and proximity for explicit configuration and separation. For descriptor-heavy work, see [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [Extending types](/docs/hotchocolate/v16/building-a-schema/extending-types/), [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/), and [Scalars](/docs/hotchocolate/v16/building-a-schema/scalars/).

## Where schema-first fits

GraphQL SDL (Schema Definition Language) is a format that tools, reviews, registries, and clients can read. It is valuable for publishing and comparing the contract:

```graphql
type Query {
  productById(id: Int!): Product
}

type Product {
  id: ID!
  name: String!
}
```

If you are used to SDL-first frameworks, separate two decisions:

| Decision | Practical meaning |
| --- | --- |
| How the team reviews the contract | SDL is excellent for review, registry workflows, schema diffs, and documentation. |
| How the Hot Chocolate server authors the contract | Most v16 application examples use C# authoring, either implementation-first or code-first. |

If your organization already owns an SDL contract, treat it as a boundary to preserve. Compare Hot Chocolate's generated SDL against that contract, validate representative operations, and only use SDL import patterns when the schema reference page for that feature provides guidance. This page focuses on the choice between implementation-first and code-first authoring.

## Working with third-party and generated types

Do not modify external packages or generated files to fit your schema.

When a type comes from outside your application, consider these options:

| Situation | Option | Why |
| --- | --- | --- |
| Generated CRM type exposes internal fields | Wrap it in an API-facing model | The public schema remains stable even if the generated source changes. |
| Third-party DTO has the right data but wrong names | Configure it with code-first | The descriptor can rename, ignore, or type fields without changing the package. |
| Most of the schema is implementation-first, but one external model needs isolation | Mix styles | The exception stays local to the external boundary. |

Favor the client-facing contract over CLR convenience. A wrapper model may require more code initially, but it provides a stable API surface when the source type changes for reasons unrelated to GraphQL.

## Planning for runtime or highly dynamic schemas

Runtime schema construction is a different challenge from choosing implementation-first or code-first for a typical application.

Dynamic schema guidance is needed when schema members come from configuration, tenant metadata, CMS fields, database-defined shapes, or tooling that generates fields at runtime. These designs add operational and testing complexity because the schema can change outside the normal code review process.

If your fields change by tenant or configuration, see [Dynamic schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas/). Avoid runtime type construction for ordinary naming, nullability, or annotation preferences.

## Setting a team convention

Choose a project default and document when exceptions apply. New contributors should be able to predict where to add a field and where to configure a type that breaks the default.

During project setup or schema review, use this checklist:

- Choose the default authoring style for new schema members.
- Write down reasons a type or field may use the other style.
- Keep file organization predictable, such as `Types/ProductQueries.cs` for resolver groups and `Types/ProductType.cs` for descriptor configuration.
- Review the generated schema output, not only the C# diff.
- Use [Nitro schema definition](/docs/nitro/documents/schema-definition/), [Nitro schema reference](/docs/nitro/documents/schema-reference/), or [Nitro schema commands](/docs/nitro/cli-commands/schema/) for shared review.
- Be consistent with names, nullability, descriptions, deprecations, directives, and error shapes.
- Decide who owns schema review for public or partner-facing APIs.

Mixing styles is fine when boundaries are clear. The problem is not mixing styles, but making every new field a fresh style decision.

## Switching styles later

You can move a type, field, or small type cluster from one style to another as a schema-preserving refactor. The process is incremental:

1. Capture the current schema output.
2. Pick a type or field with a clear reason to move.
3. Add equivalent configuration in the target style.
4. Compare the generated schema before and after the change.
5. Remove duplicated configuration.
6. Validate representative client operations.

Pay attention to details that often change during a move:
- GraphQL names
- Ignored members
- Nullability
- Scalar mappings
- Descriptions
- Deprecations
- Directives
- Middleware order
- Resolver behavior

Use [Nitro schema commands](/docs/nitro/cli-commands/schema/) for repeatable schema validation and [Nitro client commands](/docs/nitro/cli-commands/client/) to track representative client operations. For broader contract planning, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

## Decision table

| Situation | Recommended style | Why | Follow-up page |
| --- | --- | --- | --- |
| New Hot Chocolate app | Implementation-first | Matches the v16 learning path and keeps resolver intent close to code. | [At a glance](/docs/hotchocolate/v16/get-started/at-a-glance/) |
| Tutorial or learning project | Implementation-first | The quick start and tutorial use attributes, `partial` types, and `AddTypes()`. | [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field/) |
| Domain model closely matches the API | Implementation-first | Inference and attributes cover the common mapping work. | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Third-party or generated type | Code-first or wrapper model | Protect the GraphQL contract without changing external source. | [Extending types](/docs/hotchocolate/v16/building-a-schema/extending-types/) |
| Public API with strict contract review | Implementation-first default with documented exceptions | The style matters less than stable SDL output and review ownership. | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| Infrastructure package owns reusable schema rules | Code-first | Descriptor classes let the package own explicit schema configuration. | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) |
| Runtime generated schema | Dynamic schema APIs | Runtime construction is an advanced schema generation problem. | [Dynamic schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas/) |
| Existing SDL contract | Preserve and compare the SDL boundary | SDL is the contract to validate against while choosing the Hot Chocolate authoring approach. | [Schema definition in Nitro](/docs/nitro/documents/schema-definition/) |

## Next steps

- **Add your next field:** See [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field/) or [Queries](/docs/hotchocolate/v16/building-a-schema/queries/).
- **Model object types:** Read [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) and [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/).
- **Plan schema review:** See [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and inspect the SDL with [Nitro](/docs/nitro/documents/schema-definition/).
- **Design around client tasks:** Continue with [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).
- **Build dynamic schemas:** See [Dynamic schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas/).
