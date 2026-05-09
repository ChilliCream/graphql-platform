---
title: Custom descriptor attributes
---

Custom descriptor attributes let you package repeated Hot Chocolate schema configuration behind a C# attribute. Use them when your team already configures the schema with attributes and the repeated rule has a clear domain name.

This page shows how to build custom attributes for fields, arguments, types, and multiple descriptor kinds in Hot Chocolate v16.

# Before you create an attribute

A descriptor attribute is schema-build configuration. Hot Chocolate discovers the CLR attribute while building the schema, lets it configure the active descriptor, and then turns that descriptor into GraphQL schema metadata, middleware, arguments, directives, or resolver configuration.

Use a custom descriptor attribute when:

- The same descriptor configuration appears on many annotated resolvers, model members, or parameters.
- The attribute name explains the schema behavior to the reader.
- The attribute can stay small and has a clear fluent equivalent.
- Your team prefers annotation-based schema configuration for local rules.

Prefer another extension point when the rule is larger or more global:

| Need                                                       | Prefer                                                             |
| ---------------------------------------------------------- | ------------------------------------------------------------------ |
| Repeated local annotation-based configuration              | Custom descriptor attribute                                        |
| Reusable code-first configuration with explicit call sites | Fluent descriptor extension method                                 |
| Substantial type or field configuration in one place       | `ObjectType<T>`, `InputObjectType<T>`, or another descriptor class |
| Add fields without changing the source CLR type            | Type extension                                                     |
| Global or convention-based behavior                        | Convention, interceptor, or type module                            |
| Existing Hot Chocolate behavior                            | Built-in attribute with parameters                                 |

Custom attributes can hide important behavior. Keep security-sensitive rules explicit. For authorization, prefer the built-in `HotChocolate.Authorization.AuthorizeAttribute` unless your wrapper has a name that makes the policy obvious.

# Choose the right base class

Most custom attributes inherit from a specialized base class. The base class declares the allowed CLR targets and calls `OnConfigure` only for the matching descriptor type.

| Base class                             | CLR target             | Descriptor                     | Provider argument | Common use                                     |
| -------------------------------------- | ---------------------- | ------------------------------ | ----------------- | ---------------------------------------------- |
| `ObjectFieldDescriptorAttribute`       | Method or property     | `IObjectFieldDescriptor`       | `MemberInfo?`     | Object field middleware, directives, options   |
| `ObjectTypeDescriptorAttribute`        | Class or struct        | `IObjectTypeDescriptor`        | `Type?`           | Object-level directives or small type defaults |
| `ArgumentDescriptorAttribute`          | Parameter              | `IArgumentDescriptor`          | `ParameterInfo?`  | Argument type, default value, directives       |
| `InputObjectTypeDescriptorAttribute`   | Class or struct        | `IInputObjectTypeDescriptor`   | `Type?`           | Input object defaults                          |
| `InputFieldDescriptorAttribute`        | Method or property     | `IInputFieldDescriptor`        | `MemberInfo?`     | Input field defaults or directives             |
| `InterfaceTypeDescriptorAttribute`     | Class or interface     | `IInterfaceTypeDescriptor`     | `Type?`           | Interface metadata                             |
| `InterfaceFieldDescriptorAttribute`    | Method or property     | `IInterfaceFieldDescriptor`    | `MemberInfo?`     | Interface field metadata or middleware         |
| `EnumTypeDescriptorAttribute`          | Enum, class, or struct | `IEnumTypeDescriptor`          | `Type?`           | Enum metadata                                  |
| `EnumValueDescriptorAttribute`         | Field                  | `IEnumValueDescriptor`         | `FieldInfo?`      | Enum value metadata                            |
| `UnionTypeDescriptorAttribute`         | Class or interface     | `IUnionTypeDescriptor`         | `Type?`           | Union metadata                                 |
| `ScalarTypeDescriptorAttribute`        | Class                  | `IScalarTypeDescriptor`        | `Type?`           | Advanced scalar metadata                       |
| `DirectiveTypeDescriptorAttribute`     | Class or struct        | `IDirectiveTypeDescriptor`     | `Type?`           | Directive type metadata                        |
| `DirectiveArgumentDescriptorAttribute` | Property               | `IDirectiveArgumentDescriptor` | `PropertyInfo?`   | Directive argument metadata                    |

Data packages also provide filter and sort descriptor attribute base classes, such as `FilterInputTypeDescriptorAttribute`, `FilterFieldDescriptorAttribute`, `SortInputTypeDescriptorAttribute`, and `SortFieldDescriptorAttribute`.

Use `DescriptorAttribute` directly when one CLR attribute must support more than one descriptor kind, for example object fields and interface fields.

# Build a field middleware attribute

A common pattern is to write a fluent descriptor extension first, then wrap it in an attribute. That keeps attribute-based and code-first configuration aligned.

The following example adds a small middleware that requires a resolver result to be non-null. The fluent API is `UseNonNullResult()`, and the attribute wrapper is `[UseNonNullResult]`.

```csharp
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public static class NonNullResultDescriptorExtensions
{
    public static IObjectFieldDescriptor UseNonNullResult(
        this IObjectFieldDescriptor descriptor)
    {
        return descriptor.Use(next => async context =>
        {
            await next(context);

            if (context.Result is null)
            {
                throw new GraphQLException("The resolver returned null.");
            }
        });
    }
}

public sealed class UseNonNullResultAttribute : ObjectFieldDescriptorAttribute
{
    public UseNonNullResultAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.UseNonNullResult();
    }
}
```

Apply the attribute to a resolver method or property that becomes an object field:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseNonNullResult]
    public static Product GetFeaturedProduct(ProductStore store)
    {
        return store.FeaturedProduct;
    }
}

public sealed class ProductStore
{
    private readonly Product[] _products = [new() { Sku = "coffee" }];

    public Product FeaturedProduct => _products[0];

    public IQueryable<Product> Products => _products.AsQueryable();

    public IReadOnlyList<Product> Search(string category) => _products;
}

public sealed class Product
{
    public string Sku { get; set; } = default!;
}
```

The generated schema still exposes the field inferred from the resolver. The attribute changes execution behavior by adding middleware.

```graphql
type Query {
  featuredProduct: Product!
}
```

Use `ObjectFieldDescriptorAttribute` for object fields. It does not configure interface fields. If the same attribute must work on interface fields, use `DescriptorAttribute` and handle `IInterfaceFieldDescriptor` explicitly.

# Preserve middleware order

Hot Chocolate applies descriptor attributes during schema building. When multiple descriptor configurations are present, it sorts them by `DescriptorAttribute.Order`. Do not depend on raw C# reflection order.

Middleware attributes should use the line-number pattern used by Hot Chocolate built-ins:

```csharp
public UseNonNullResultAttribute([CallerLineNumber] int order = 0)
{
    Order = order;
}
```

This maps the attribute declaration line to `Order`, which gives a stable order for attributes written on the same member.

When you combine the built-in data middleware attributes, keep the required source order:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products;
}
```

If your custom attribute adds field middleware and composes with built-in middleware, document where it belongs in the stack. Inherited attributes can make ordering harder to review, so pass the `order` constructor parameter through when deriving from another middleware attribute.

# Build an argument attribute

Use `ArgumentDescriptorAttribute` for resolver parameters that become GraphQL arguments. This example adds a default value to an argument.

```csharp
#nullable enable

using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public sealed class ArgumentDefaultValueAttribute : ArgumentDescriptorAttribute
{
    public ArgumentDefaultValueAttribute(object value)
    {
        Value = value;
    }

    public object Value { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo? parameter)
    {
        descriptor.DefaultValue(Value);
    }
}
```

Apply it to the resolver parameter:

```csharp
[QueryType]
public static partial class SearchQueries
{
    public static IReadOnlyList<Product> SearchProducts(
        [ArgumentDefaultValue("all")] string category,
        ProductStore store)
    {
        return store.Search(category);
    }
}
```

Expected SDL:

```graphql
type Query {
  searchProducts(category: String! = "all"): [Product!]!
}
```

Prefer constructor arguments or attribute properties for data the attribute needs. Read `ParameterInfo?` only when the rule cannot be expressed through explicit attribute parameters.

# Build a type-level attribute

Use `ObjectTypeDescriptorAttribute` for focused object-level configuration. Keep type-level attributes small because they can hide schema changes from reviewers.

This attribute adds a common object description:

```csharp
#nullable enable

using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public sealed class CatalogTypeAttribute : ObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type? type)
    {
        descriptor.Description("Part of the catalog API.");
    }
}
```

Apply it to the CLR type:

```csharp
[CatalogType]
public sealed class Product
{
    public string Sku { get; set; } = default!;
}
```

Expected SDL:

```graphql
"""
Part of the catalog API.
"""
type Product {
  sku: String!
}
```

If the attribute adds fields, changes binding behavior, or contains conditional logic, consider a descriptor class instead. A descriptor class is more visible during review and can hold longer configuration without turning the CLR type into a dense list of annotations.

# Support multiple descriptor types

Use `DescriptorAttribute` directly when one annotation must work with more than one descriptor kind. You must provide `AttributeUsage` yourself and override `TryConfigure`.

This example applies the same deprecation reason to object fields and interface fields:

```csharp
#nullable enable

using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Property,
    Inherited = true,
    AllowMultiple = false)]
public sealed class DeprecatedFieldAttribute : DescriptorAttribute
{
    public DeprecatedFieldAttribute(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        switch (descriptor)
        {
            case IObjectFieldDescriptor objectField:
                objectField.Deprecated(Reason);
                break;

            case IInterfaceFieldDescriptor interfaceField:
                interfaceField.Deprecated(Reason);
                break;
        }
    }
}
```

Apply it to a member that becomes a field:

```csharp
public interface IProduct
{
    [DeprecatedField("Use sku instead.")]
    string LegacySku { get; }

    string Sku { get; }
}
```

Expected SDL:

```graphql
interface Product {
  legacySku: String! @deprecated(reason: "Use sku instead.")
  sku: String!
}
```

Ignore unsupported descriptor types when the attribute can be harmless on extra targets. Throw a clear exception only when misuse should fail schema building.

# Handle nullable providers in v16

In Hot Chocolate v16, descriptor attribute provider values are nullable:

- `TryConfigure(..., ICustomAttributeProvider? attributeProvider)`
- `OnConfigure(..., MemberInfo? member)`
- `OnConfigure(..., Type? type)`
- `OnConfigure(..., ParameterInfo? parameter)`
- `OnConfigure(..., FieldInfo? field)`
- `OnConfigure(..., PropertyInfo? property)`

This supports schema building paths that are not purely reflection-based. Write attributes so they can configure the descriptor without reflection metadata when possible.

```csharp
protected override void OnConfigure(
    IDescriptorContext context,
    IObjectFieldDescriptor descriptor,
    MemberInfo? member)
{
    if (member is not null)
    {
        descriptor.Description($"Configured from {member.Name}.");
    }
    else
    {
        descriptor.Description("Configured by the custom attribute.");
    }
}
```

Set `RequiresAttributeProvider = true` only when the attribute cannot configure correctly without the provider. If you set it, document the requirement because it can limit source-generated or non-reflection scenarios.

# Use descriptor context safely

`IDescriptorContext` is a schema-build context, not a request context. It gives access to schema options, naming and type conventions, the type inspector, schema-build services, descriptor factories, resolver compilation, and schema-created callbacks.

Use `context.Services` for services that are valid during schema construction. Do not resolve request-scoped services in an attribute constructor or store request data on an attribute instance. For request-time work, add field middleware and use `IMiddlewareContext`, resolver parameters, or dependency injection in the resolver.

# Compose with built-in behavior

Prefer calling fluent descriptor APIs from a custom attribute. This keeps the equivalent code-first configuration visible:

```csharp
public static IObjectFieldDescriptor UseCatalogList(
    this IObjectFieldDescriptor descriptor)
{
    return descriptor
        .UsePaging(options => options.MaxPageSize = 100)
        .UseProjection()
        .UseFiltering()
        .UseSorting();
}

public sealed class UseCatalogListAttribute : ObjectFieldDescriptorAttribute
{
    public UseCatalogListAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.UseCatalogList();
    }
}
```

This kind of wrapper is useful when the name represents a project concept. It is a poor fit when readers need to inspect the attribute source to learn which middleware was added. Document the middleware order and required schema registrations, such as filtering, sorting, projections, or paging providers.

`DescriptorAttribute.ApplyAttribute<T>` exists for applying another descriptor attribute from inside your attribute. Prefer fluent descriptor APIs in new code because `ApplyAttribute<T>` requires a non-null provider and can make behavior harder to review.

# Test custom descriptor attributes

Test the schema or runtime behavior caused by the attribute. Avoid tests that only assert the schema object is non-null.

A schema test can build the schema and snapshot the SDL:

```csharp
#nullable enable

using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

public sealed class AttributeTests
{
    [Fact]
    public async Task ArgumentDefaultValueAttribute_Should_Add_Default_Value()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<SearchQueries>()
            .BuildRequestExecutorAsync();

        // act
        var schema = executor.Schema;

        // assert
        schema.MatchInlineSnapshot(
            """
            schema {
              query: SearchQueries
            }

            type SearchQueries {
              searchProducts(category: String! = "all"): [Product!]!
            }

            type Product {
              sku: String!
            }
            """);
    }
}
```

For middleware attributes, execute a representative query and snapshot the result. For ordering-sensitive attributes, write a focused test where the result proves the order.

# Troubleshoot custom attributes

| Problem                                                                         | What to check                                                                                                                                                          |
| ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The attribute never runs.                                                       | Check `AttributeUsage`, the CLR target, whether the type is part of the schema, and whether the descriptor type matches the base class.                                |
| It runs on object fields but not interface fields.                              | `ObjectFieldDescriptorAttribute` handles object fields only. Use `DescriptorAttribute` and handle `IInterfaceFieldDescriptor`, or create an interface field attribute. |
| Middleware runs in the wrong order.                                             | Check `Order`, `[CallerLineNumber]`, inherited attributes, and source order.                                                                                           |
| `member`, `parameter`, or `type` is null.                                       | v16 allows nullable providers. Null-check the provider or set `RequiresAttributeProvider = true` when reflection metadata is required.                                 |
| Services are unavailable or scoped services fail.                               | Attribute configuration runs during schema building. Move request-scoped work into middleware or resolvers.                                                            |
| The schema changed but reviewers cannot see why.                                | The attribute hides too much. Prefer fluent configuration, a descriptor class, or a smaller attribute.                                                                 |
| The attribute works in reflection-based tests but fails with source generation. | Remove the reflection dependency, null-check the provider, or document and enforce `RequiresAttributeProvider`.                                                        |

# Next steps

- Review the [attributes overview](./index) for built-in attribute order and selection guidance.
- Use [field middleware](../../execution-engine/field-middleware) when your attribute adds execution behavior.
- Use [object type descriptors](../../building-a-schema/object-types) and [arguments](../../building-a-schema/arguments) for the fluent APIs your attributes wrap.
- Use [testing](../../guides/testing) to snapshot generated SDL or execution results.
