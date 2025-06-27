---
title: Migrate Hot Chocolate from 15 to 16
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 16.

Start by installing the latest `16.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Skip/include disallowed on root subscription fields

The `@skip` and `@include` directives are now disallowed on root subscription fields, as specified in the RFC: [Prevent @skip and @include on root subscription selection set](https://github.com/graphql/graphql-spec/pull/860).

## Deprecation of fields not deprecated in the interface

Deprecating a field now requires the implemented field in the interface to also be deprecated, as specified in the [draft specification](https://spec.graphql.org/draft/#sec-Objects.Type-Validation).

## Accidental use of `Microsoft.AspNetCore.Authorization.*` attributes throws an error

Since our authorization attributes (`[Authorize]` and `[AllowAnonymous]`) share the same names as the regular ASP.NET attributes, it's easy to accidentally use the wrong ones.
In the worst-case scenario, this could result in your field or type ending up without any authorization being applied!

To prevent this, we've added a check that throws an error during schema generation if it detects `Microsoft.AspNetCore.Authorization.*` attributes being applied to a GraphQL resolver.

> Note: Keep in mind that your clients might currently rely on the absence of authorization.

You can disable this new validation by setting the `ErrorOnAspNetCoreAuthorizationAttributes` option to `false`:

```csharp
builder.Services.AddGraphQLServer()
    .ModifyOptions(options => {
        options.ErrorOnAspNetCoreAuthorizationAttributes = false;
    })
```

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
