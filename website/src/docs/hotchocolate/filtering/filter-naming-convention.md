---
title: Filtering - Naming Convention
---

_Hot Chococlate_ already provides two naming scheme for filters. If you would like to define your own naming scheme or extend existing ones have a look at the documentation of <<LINk FILTER CONVENTIONS>>


### Snake Case

**Configuration**
You can configure the Snake Case with the `UseSnakeCase` extension method convention on the `IFilterConventionDescriptor`

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.UseSnakeCase()
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => x.UseSnakeCase())
```

```graphql
input FooBarFilter {
  AND: [FooBarFilter!]
  nested: String
  nested_contains: String
  nested_ends_with: String
  nested_in: [String]
  nested_not: String
  nested_not_contains: String
  nested_not_ends_with: String
  nested_not_in: [String]
  nested_not_starts_with: String
  nested_starts_with: String
  OR: [FooBarFilter!]
}

input FooFilter {
  AND: [FooFilter!]
  bool: Boolean
  bool_not: Boolean
  comparable: Short
  comparableEnumerable_all: ISingleFilterOfInt16Filter
  comparableEnumerable_any: Boolean
  comparableEnumerable_none: ISingleFilterOfInt16Filter
  comparableEnumerable_some: ISingleFilterOfInt16Filter
  comparable_gt: Short
  comparable_gte: Short
  comparable_in: [Short!]
  comparable_lt: Short
  comparable_lte: Short
  comparable_not: Short
  comparable_not_gt: Short
  comparable_not_gte: Short
  comparable_not_in: [Short!]
  comparable_not_lt: Short
  comparable_not_lte: Short
  object: FooBarFilter
  OR: [FooFilter!]
}

input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  element: Short
  element_gt: Short
  element_gte: Short
  element_in: [Short!]
  element_lt: Short
  element_lte: Short
  element_not: Short
  element_not_gt: Short
  element_not_gte: Short
  element_not_in: [Short!]
  element_not_lt: Short
  element_not_lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```

### Pascal Case

**Configuration**
You can configure the Pascal Case with the `UsePascalCase` extension method convention on the `IFilterConventionDescriptor`

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.UsePascalCase()
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => x.UsePascalCase())
```

```graphql
input FooBarFilter {
  AND: [FooBarFilter!]
  Nested: String
  Nested_Contains: String
  Nested_EndsWith: String
  Nested_In: [String]
  Nested_Not: String
  Nested_Not_Contains: String
  Nested_Not_EndsWith: String
  Nested_Not_In: [String]
  Nested_Not_StartsWith: String
  Nested_StartsWith: String
  OR: [FooBarFilter!]
}

input FooFilter {
  AND: [FooFilter!]
  Bool: Boolean
  Bool_Not: Boolean
  Comparable: Short
  ComparableEnumerable_All: ISingleFilterOfInt16Filter
  ComparableEnumerable_Any: Boolean
  ComparableEnumerable_None: ISingleFilterOfInt16Filter
  ComparableEnumerable_Some: ISingleFilterOfInt16Filter
  Comparable_Gt: Short
  Comparable_Gte: Short
  Comparable_In: [Short!]
  Comparable_Lt: Short
  Comparable_Lte: Short
  Comparable_Not: Short
  Comparable_Not_Gt: Short
  Comparable_Not_Gte: Short
  Comparable_Not_In: [Short!]
  Comparable_Not_Lt: Short
  Comparable_Not_Lte: Short
  Object: FooBarFilter
  OR: [FooFilter!]
}

input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  Element: Short
  Element_Gt: Short
  Element_Gte: Short
  Element_In: [Short!]
  Element_Lt: Short
  Element_Lte: Short
  Element_Not: Short
  Element_Not_Gt: Short
  Element_Not_Gte: Short
  Element_Not_In: [Short!]
  Element_Not_Lt: Short
  Element_Not_Lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```
