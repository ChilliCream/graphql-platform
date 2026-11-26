---
title: Migrate Hot Chocolate from 16.7 to 16.8
metaTitle: "Hot Chocolate 16.8 Migration Guide"
description: "Migration guide for Hot Chocolate v16.7 to v16.8: review code that assumes non-empty selection sets, now that empty selection sets are valid by default, review operation costs, now that fields added by type interceptors get the default resolver cost, update code that compares directive argument coordinates, which now name the directive, and fix directives that reference themselves indirectly, which schema validation now rejects."
---

Update every `HotChocolate.*` package in the application to version 16.8 before applying these changes.

# Deprecations

## GraphQL command-line host extensions

`RunWithGraphQLCommands` and `RunWithGraphQLCommandsAsync` are obsolete in 16.8.0. Both methods still work on `IHost` and `IHostBuilder` through the `HotChocolate.AspNetCore.CommandLine` package.

To migrate, keep the `HotChocolate.AspNetCore.CommandLine` package reference in the application project. Its generator includes the schema commands in the shared runner. Return the exit code from the shared runner in `Program.cs`.

```diff
- return await app.RunWithGraphQLCommandsAsync(args);
+ return await app.RunWithCommandsAsync(args);
```

For synchronous callers, replace `return app.RunWithGraphQLCommands(args);` with `return app.RunWithCommands(args);`. Call either method on the built `IHost`, including a `WebApplication`.

The shared runner reports unknown command names as errors. To start the host with configuration overrides, pass key/value pairs such as `--urls http://localhost:5000` or `Logging:LogLevel:Default=Debug`.

## Draft20230127 transport version deprecated

`HttpTransportVersion.Draft20230127` is obsolete. It selects the same behavior as `Draft20250508`. Use `Latest`, which resolves to the server's default version, or `Draft20250508` to keep the current behavior, see [Transport Versions](../server/http-transport.md#transport-versions).

## Draft20260903 transport version renamed to September2026

`HttpTransportVersion.Draft20260903` is obsolete. Use `HttpTransportVersion.September2026`, which names the [September 2026 edition](https://http-spec.graphql.org/September2026/) of the GraphQL over HTTP specification and selects the same behavior, see [Transport Versions](../server/http-transport.md#september2026).

## EnableEmptySelectionSets deprecated

`SchemaOptions.EnableEmptySelectionSets`, `IReadOnlySchemaOptions.EnableEmptySelectionSets`, and `ValidationOptions.EnableEmptySelectionSets` are obsolete and will be removed in version 17. Remove settings that set them to `true`, which is now the default, see [Empty selection sets are valid by default](#empty-selection-sets-are-valid-by-default).

# Behavioral breaking changes

## Directive argument coordinates name the directive

The schema coordinate of a directive argument has the form `@directive(argument:)`, as in `@cost(weight:)`. In 16.7, `DirectiveArgument.Coordinate` returned `cost.weight`, and errors named the argument that way: schema errors such as an incompatible default value, and the `coordinate` extension of a request error for an invalid directive argument value. Update code and clients that compare directive argument coordinates with the old form.

## Directives that reference themselves indirectly are rejected

A directive definition must not apply a directive that leads back to the definition through types or other directives, as the [GraphQL specification](https://spec.graphql.org/September2026/#sec-Type-System.Directives.Type-Validation) requires. In 16.7, schema validation rejected only a directive that applied itself directly. A schema like the following now fails to build:

```graphql
directive @a(arg: Int @b) on INPUT_FIELD_DEFINITION

directive @b(arg: I) on ARGUMENT_DEFINITION

input I {
  f: Int @a
}
```

```text
Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@b(arg:)', 'I.f'.
```

The path lists each reference from the directive back to itself: here, the argument `arg` of `@a` applies `@b`, whose argument `arg` has the type `I`, whose field `f` applies `@a`. Change the schema so that no such path leads back to the directive. A cycle through directives applied to directive definitions alone, such as `directive @a @b` with `directive @b @a`, still fails earlier, with an error that the dependencies of the directives cannot be resolved. The check runs when `StrictValidation` is enabled, which is the default.

## Empty selection sets are valid by default

Validation accepts empty selection sets (`{ }`) on composite fields, on fragments, and on query and mutation operations, as the [September 2026 edition](https://spec.graphql.org/September2026/) of the GraphQL specification allows. In 16.7, `EnableEmptySelectionSets` defaulted to `false`, and such an operation failed validation. A field with an empty selection set resolves to an empty object, such as `{ "hero": {} }`, and an empty query or mutation returns `{ "data": {} }`. An empty subscription operation is still invalid.

Review custom code that assumes a composite field selects at least one field, such as validation rules or resolvers that inspect the selection set. To keep rejecting empty selection sets, for example while other tools that process the same documents reject them, set the obsolete option to `false`:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableEmptySelectionSets = false);
```

## Fields added by type interceptors get the default resolver cost

A field that a type interceptor adds gets the [default resolver cost](../security/cost-analysis.md#how-cost-is-calculated) of `10` when it has no pure resolver, whatever the order in which the cost analyzer and the interceptor are registered. In 16.7, when the cost analyzer was registered first, which `AddGraphQLServer()` does, such a field had no `@cost` directive and was priced like a field without a resolver.

These fields now have a field cost of `10` per selection, where 16.7 charged `1` for a field that returns an object, interface, or union type, and `0` otherwise:

- `_entities`, which `AddApolloFederation()` adds to the query type.
- `query`, which `AddQueryFieldToMutationPayloads()` adds to mutation payload types.
- Fields with a resolver that an application's own `TypeInterceptor` adds.

An operation whose field cost was close to `MaxFieldCost` can now exceed it and be rejected with the error code `HC0047`. [Raise `MaxFieldCost`](../security/cost-analysis.md#adjust-maxfieldcost-and-maxtypecost) if such operations must still run. To keep a lower cost for a field that your own interceptor adds, give the field an explicit `@cost` directive, which the cost analyzer does not replace.
