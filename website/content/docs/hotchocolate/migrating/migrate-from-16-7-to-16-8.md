---
title: Migrate Hot Chocolate from 16.7 to 16.8
metaTitle: "Hot Chocolate 16.8 Migration Guide"
description: "Migration guide for Hot Chocolate v16.7 to v16.8: review operation costs, now that fields added by type interceptors get the default resolver cost."
---

Update every `HotChocolate.*` package in the application to version 16.8 before applying these changes.

# Behavioral breaking changes

## Fields added by type interceptors get the default resolver cost

A field that a type interceptor adds gets the [default resolver cost](../security/cost-analysis.md#how-cost-is-calculated) of `10` when it has no pure resolver, whatever the order in which the cost analyzer and the interceptor are registered. In 16.7, when the cost analyzer was registered first, which `AddGraphQLServer()` does, such a field had no `@cost` directive and was priced like a field without a resolver.

These fields now have a field cost of `10` per selection, where 16.7 charged `1` for a field that returns an object, interface, or union type, and `0` otherwise:

- `_entities`, which `AddApolloFederation()` adds to the query type.
- `query`, which `AddQueryFieldToMutationPayloads()` adds to mutation payload types.
- Fields with a resolver that an application's own `TypeInterceptor` adds.

An operation whose field cost was close to `MaxFieldCost` can now exceed it and be rejected with the error code `HC0047`. [Raise `MaxFieldCost`](../security/cost-analysis.md#adjust-maxfieldcost-and-maxtypecost) if such operations must still run. To keep a lower cost for a field that your own interceptor adds, give the field an explicit `@cost` directive, which the cost analyzer does not replace.
