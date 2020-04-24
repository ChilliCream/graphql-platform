# Customizing Filter

Hot Chocolate provides different APIs to customize filtering. You can write custom filter input types, customize the inference behaviour of .NET Objects, customize the generated expression or create a custom visitor and attach your exotic database.

**As this can be a bit overwhelming the following questionnaire might help:**

|                                                                                                                            |                               |
| -------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| _You do not want all of the generated filters and only allow a particular set of filters in a specific case?_              | Custom&nbsp;FilterInputType   |
| _You want to change the name of a field or a whole type?_                                                                  | Custom&nbsp;FilterInputType   |
| _You want to configure how the name and the description of filters are generated in general? e.g. `PascalCaseFilterType`?_ | Filter&nbsp;Conventions       |
| _You want to configure what filters are allowed in general?_                                                               | Filter&nbsp;Conventions       |
| _You want to change the naming of a particular filter type? e.g._ `foo_contains` _should be_ `foo_like`                    | Filter&nbsp;Conventions       |
| _You want to customize the expression a filter is generating: e.g._ `_equals` _should not be case sensitive?_              | Expression&nbsp;Visitor&nbsp; |
| _You want to create your own filter types with custom parameters and custom expressions? e.g. GeoJson?_                    | Filter&nbsp;Conventions       |
| _You have a database client that does not support `IQueryable` and wants to generate filters for it?_                      | Custom&nbsp;Visitor           |

## Custom&nbsp;FilterInputType

Under the hood, filtering is based on top of normal _Hot Chocolate_ input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal filter input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

```csharp
public class Foo
{
    public string Name {get; set; }

    public string LastName {get; set; }
}

public class FooFilterType
    : FilterInputType<Foo>
{
    protected override void Configure( IFilterInputTypeDescriptor<Foo> descriptor) {

    }
}
```

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>` and adds the configuration interface for the filters. By default filters for all fields of the type are generated.
If you do want to specify the filters by yourself you can change this behaviour with `BindFields`, `BindFieldsExplicitly` or `BindFieldsImplicitly`.

```csharp
public class FooFilterType
    : FilterInputType<Foo>
{
    protected override void Configure( IFilterInputTypeDescriptor<Foo> descriptor) {
       descriptor.BindFieldsExplicitly();
       descriptor.Filter(x => x.Name);
    }
}
```

```graphql
input FooFilter {
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
  AND: [FooFilter!]
  OR: [FooFilter!]
}
```

To add or customize a filter you have to use `Filter(x => x.Foo)` for scalars `List(x => x.Bar)` for lists and `Object(x => x.Baz)` for nested objects.
These methods will return fluent interfaces to configure the filter for the selected field.

A field has different filter operations that can be configured. You will find more about filter types and filter operations here <<LINK>>
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
Operations on fields can again be bound implicitly or explicitly. By default, operations are generated for all fields of the type.
If you do want to specify the operations by yourself you can change this behaviour with `BindFilters`, `BindFiltersExplicitly` or `BindFiltersImplicitly`.

It is also possible to customize the GraphQL field of the operation further. You can change the name, add a description or directive.

```csharp
public class FooFilterType
    : FilterInputType<Foo>
{
    protected override void Configure( IFilterInputTypeDescriptor<Foo> descriptor) {
       // descriptor.BindFieldsImplicitly(); <- is already the default
       descriptor.Filter(x => x.Foo)
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
input FooFilter {
  """
  Checks if the provided string is contained in the `Name` of a User
  """
  exits_with_name: String @name
  foo_contains: String
  AND: [FooFilter!]
  OR: [FooFilter!]
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

# Filter Conventions

The customization of filters with `FilterInputTypes<T>` works if you only want to customize one specific filter type.
If you want to change the behaviour of all filter types, you want to create a convention for your filters. The filter convention comes with a fluent interface that is really close to a type descriptor.
You can see the convention as a configuration object that holds state that is used from the types system or the execution engine.

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
