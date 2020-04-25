---
title: Filtering - Customizing Filter
---

## Customizing Filter

Hot Chocolate provides different APIs to customize filtering. You can write custom filter input types, customize the inference behavior of .NET Objects, customize the generated expression, or create a custom visitor and attach your exotic database.

**As this can be a bit overwhelming the following questionnaire might help:**

|                                                                                                                            |                                 |
| -------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| _You do not want all of the generated filters and only allow a particular set of filters in a specific case?_              | Custom&nbsp;FilterInputType     |
| _You want to change the name of a field or a whole type?_                                                                  | Custom&nbsp;FilterInputType     |
| _You want to change the name of the `where` argument?_                                                                     | Filter Conventions ArgumentName |
| _You want to configure how the name and the description of filters are generated in general? e.g. `PascalCaseFilterType`?_ | Filter&nbsp;Conventions         |
| _You want to configure what filters are allowed in general?_                                                               | Filter&nbsp;Conventions         |
| _You want to change the naming of a particular filter type? e.g._ `foo_contains` _should be_ `foo_like`                    | Filter&nbsp;Conventions         |
| _You want to customize the expression a filter is generating: e.g._ `_equals` _should not be case sensitive?_              | Expression&nbsp;Visitor&nbsp;   |
| _You want to create your own filter types with custom parameters and custom expressions? e.g. GeoJson?_                    | Filter&nbsp;Conventions         |
| _You have a database client that does not support `IQueryable` and wants to generate filters for it?_                      | Custom&nbsp;Visitor             |

## Custom&nbsp;FilterInputType

Under the hood, filtering is based on top of normal _Hot Chocolate_ input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal filter input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

```csharp
public class User
{
    public string Name {get; set; }

    public string LastName {get; set; }
}

public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {

    }
}
```

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>` and adds the configuration interface for the filters. By default filters for all fields of the type are generated.
If you do want to specify the filters by yourself you can change this behavior with `BindFields`, `BindFieldsExplicitly` or `BindFieldsImplicitly`.

```csharp
public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {
       descriptor.BindFieldsExplicitly();
       descriptor.Filter(x => x.Name);
    }
}
```

```graphql
input UserFilter {
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  name_not: String
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

To add or customize a filter you have to use `Filter(x => x.Foo)` for scalars `List(x => x.Bar)` for lists and `Object(x => x.Baz)` for nested objects.
These methods will return fluent interfaces to configure the filter for the selected field.

A field has different filter operations that can be configured. You will find more about filter types and filter operations here <<LINK>>
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
Operations on fields can again be bound implicitly or explicitly. By default, operations are generated for all fields of the type.
If you do want to specify the operations by yourself you can change this behavior with `BindFilters`, `BindFiltersExplicitly` or `BindFiltersImplicitly`.

It is also possible to customize the GraphQL field of the operation further. You can change the name, add a description or directive.

```csharp
public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {
       // descriptor.BindFieldsImplicitly(); <- is already the default
       descriptor.Filter(x => x.Name)
          .BindFilterExplicitly()
          .AllowContains()
            .Description("Checks if the provided string is contained in the `Name` of a User")
            .And()
          .AllowEquals()
            .Name("exits_with_name")
            .Directive("name");
       descriptor.Ignore(x => x.Bar);
    }
}
```

```graphql
input UserFilter {
  exits_with_name: String @name
  """
  Checks if the provided string is contained in the `Name` of a User
  """
  name_contains: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

**API Documentation**

| Method                                                                           | Description                                                                                                                                     |
| -------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `csharp±BindFields(BindingBehavior bindingBehavior)`                             | Defines the filter binding behavior. `Explicitly`or `Implicitly`. Default is `Implicitly`                                                       |
| `csharp±BindFieldsExplicitly`                                                    | Defines that all filters have to be specified explicitly. This means that only the filters are applied that are added with `Filter(x => x.Foo)` |
| `csharp±BindFieldsImplicitly`                                                    | The filter type will add filters for all compatible fields.                                                                                     |
| `csharp±Description(string value)`                                               | Adds explanatory text of the `FilterInputType<T>` that can be accessed via introspection.                                                       |
| `csharp±Name(NameString value)`                                                  | Defines the graphql name of the `FilterInputType<T>`.                                                                                           |
| `csharp±Ignore( Expression<Func<T, object>> property);`                          | Ignore the specified property.                                                                                                                  |
| `csharp±Filter( Expression<Func<T, string>> property)`                           | Defines a string filter for the selected property.                                                                                              |
| `csharp±Filter( Expression<Func<T, bool>> property)`                             | Defines a bool filter for the selected property.                                                                                                |
| `csharp±Filter( Expression<Func<T, IComparable>> property)`                      | Defines a comarable filter for the selected property.                                                                                           |
| `csharp±Object<TObject>( Expression<Func<T, TObject>> property)`                 | Defines a object filter for the selected property.                                                                                              |
| `csharp±List( Expression<Func<T, IEnumerable<string>>> property)`                | Defines a array string filter for the selected property.                                                                                        |
| `csharp±List( Expression<Func<T, IEnumerable<bool>>> property)`                  | Defines a array bool filter for the selected property.                                                                                          |
| `csharp±List( Expression<Func<T, IEnumerable<IComparable>>> property)`           | Defines a array comarable filter for the selected property.                                                                                     |
| `csharp±Filter<TObject>( Expression<Func<T, IEnumerable<TObject>>> property)`    | Defines a array object filter for the selected property.                                                                                        |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive `directiveInstance` to the type                                                                                                   |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive of type `TDirective` to the type                                                                                                  |
| `csharp±Directive<TDirective>(NameString name, params ArgumentNode[] arguments)` | Add directive of type `TDirective` to the type                                                                                                  |

---

# Filter Conventions

The customization of filters with `FilterInputTypes<T>` works if you only want to customize one specific filter type.
If you want to change the behavior of all filter types, you want to create a convention for your filters. The filter convention comes with a fluent interface that is close to a type descriptor.
You can see the convention as a configuration object that holds the state that is used by the type system or the execution engine.

## Get Started

To use a filter convention you can extend `FilterConvention` and override the `Configure` method. Alternatively, you can directly configure the convention over the constructor argument.
You then have to register your custom convention on the schema builder with `AddConvention`.

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => /*Config*/));
```

---

## Convention Descriptor Basics

In this section, we will take a look at the basic features of the filter convention.
The documentation will reference often to `descriptor`. Imagine this `descriptor` as the parameter of the Configure method of the filter convention in the following context:

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(
       /**highlight-start**/
        IFilterConventionDescriptor descriptor
      /**highlight-end**/
     )
     {}
}

SchemaBuilder.New().AddConvention<CustomConvention>();
```

<br/>

##### Argument Name

With the convention descriptor, you can easily change the argument name of the `FilterInputType`.

**Configuration**

```csharp
descriptor.ArgumentName("example_argument_name");
```

**Result**

```graphql
type Query {
  users(example_argument_name: UserFilter): [User]
}
```

##### Change Name of Scalar List Type Element

You can change the name of the element of the list type.

**Configuration**

```csharp
descriptor.ElementName("example_element_name");
```

**Result**

```graphql
input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  example_element_name: Short
  example_element_name_gt: Short
  example_element_name_gte: Short
  example_element_name_in: [Short!]
  example_element_name_lt: Short
  example_element_name_lte: Short
  example_element_name_not: Short
  example_element_name_not_gt: Short
  example_element_name_not_gte: Short
  example_element_name_not_in: [Short!]
  example_element_name_not_lt: Short
  example_element_name_not_lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```

##### Configure Filter Type Name Globally

To change the way filter types are named, you have to exchange the factory.

You have to provide a delegate of the following type:

```csharp
public delegate NameString GetFilterTypeName(
    IDescriptorContext context,
    Type entityType);
```

**Configuration**

```csharp
descriptor.TypeName(
    (context,types) => context.Naming.GetTypeName(entityType, TypeKind.Object) + "Custom");
```

**Result**

```graphql
type Query {
  users(where: UserCustom): [User]
}
```

##### Configure Filter Description Globally

To change the way filter types are named, you have to exchange the factory.

You have to provide a delegate of the following type:

```csharp
public delegate string GetFilterTypeDescription(
    IDescriptorContext context,
    Type entityType);
```

**Configuration**

```csharp
descriptor.TypeName(
    (context,types) => context.Naming.GetTypeDescription(entityType, TypeKind.Object); + "Custom");
```

**Result**

```graphql
"""
Custom
"""
input UserFilter {
  AND: [UserFilter!]
  isOnline: Boolean
  isOnline_not: Boolean
  OR: [UserFilter!]
}
```

##### Reset Configuration

By default, all predefined values are configured. To start from scratch, you need to call `Reset()`first.

**Configuration**

```csharp
descriptor.Reset();
```

**Result**

> **⚠ Note:** You will need to add a complete configuration, otherwise the filter will not work as desired!

---

## Describe with convention

With the filter convention descriptor, you have full control over what filters are inferred, their names, operations, and a lot more.
The convention provides a familiar interface to the type configuration. It is recommended to first take a look at `Filter & Operations` to understand the concept of filters. This will help you understand how the filter configuration works.

Filtering has two core components at its heart. First, you have the inference of filters based on .NET types. The second component is an interceptor that translates the filters to the desired output and applies it to the resolver pipeline. These two parts can (and have to) be configured completely independently. With this separation, it is possible to easily extend the behavior. The descriptor is designed to be extendable by extension methods.

### Configuration of the type system

In this section, we will focus mainly on the generation of the schema. If you are interested in changing how filters are translated to the database, you have to look here <<INSERT LINK HERE>>

##### Configure Filter Operations

Operations can be configured in two ways.

You can configure a default configuration that applies to all operations of this kind. In this case the configuration for `FilterOperationKind.Equals` would be applied to all `FilterKind` that specify this operation.

```csharp
 descriptor.Operation(FilterOperationKind.Equals)
```

If you want to configure a more specific Operation e.g. `FilterOperationKind.Equal` of kind `FilterKind.String`, you can override the default behavior.

```csharp
 descriptor.Type(FilterKind.String).Operation(FilterOperationKind.Equals)
```

The operation descriptor allows you to configure the name, the description or even ignore an operation completely

In this example, we will look at the following input type:

```graphql
input UserFilter {
  loggingCount: Int
  loggingCount_gt: Int
  loggingCount_gte: Int
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  loggingCount_not: Int
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  name_not: String
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

**Change the name of an operation**

To change the name of an operation you need to specify a delegate of the following type:

```csharp
public delegate NameString CreateFieldName(
    FilterFieldDefintion definition,
    FilterOperationKind kind);
```

**Configuration**

```csharp{1, 6}
 // (A)
 // specifies that all not equals operations should be extended with _nada
 descriptor
    .Operation(FilterOperationKind.NotEquals)
        .Name((def, kind) => def.Name + "_nada" );
 // (B)
 // specifies that the not equals operations should be extended with _niente.
 // this overrides (A)
 descriptor
    .Type(FilterKind.Comparable)
        .Operation(FilterOperationKind.NotEquals)
            .Name((def, kind) => def.Name + "_niente" )
```

**after**

```graphql{8,18}
input UserFilter {
  loggingCount: Int
  loggingCount_gt: Int
  loggingCount_gte: Int
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  loggingCount_niente: Int   <-- (B)
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  name_nada: String  <-- (A)
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```
