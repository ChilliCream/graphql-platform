---
title: Extending Filtering
---
> **Work in progress**: This documentation is not yet complete.

The `HotChocolate.Data` package works with all databases that support `IQueryable`. Included in the
default settings, are all filter operations that work over `IQueryable` on all databases. 
Sometimes this is not enough. Some databases might not support `IQueryable`. Some other databases may have
technology-specific operations (e.g. SQL Like). Filtering was designed with extensibility in mind.

Filtering can be broken down into two basic parts. Schema building and execution. In schema building,
the input types are created. In execution, the data passed by the user is analyzed and translated to a 
database query. Both parts can be configured over a convention.

In theory, you are free to design the structure of filters as it suits you best. 
Usually, it makes sense to divide the structure into two parts. The _field_ and the _operation_.

The query below returns all movies where the franchise is equal to "Star Wars". The _field_ `franchise` where the filter
is applied to and the _operation_ equals (`eq`) that should operate on this field.
```
{
    movies(
        where: { 
            franchise: {
                eq: "Star Wars"
            }
        }) {
        name
    }
}
```

Fields can also form paths. In the query below there are two _fields_ `genre` and `totalMovieCount` and one operation equals
`eq`
```
{
    movies(
        where: { 
            genre: {
                totalMovieCount: {
                    eq: 100
                }
            }
        }) {
        name
    }
}
```

The two queries above show the difference between _fields_ and _operations_ well. A field is always context-specific. 
Even when two fields have the same name, like the description of a movie and the description of a genre, they have different meanings.
One field refers to the description of a movie and the other description refers to the description of a genre.
Same name, different meanings. An operation on the other hand, has always the same meaning. 
The equals operation (`eq`) do always mean that the value of the selected field, should
be equals to the value that was provided in the query. 
Operations can be applied in different contexts, but the operation itself, stays the same.
The name of the operation should be consistent. There should only be one operation that checks for equality. 
This operation should always have the same name.

With this in mind, we can have a deeper dive into filtering. Buckle up, this might get exciting.

# How everything fits together
At the core of the configuration API of filtering there sits a convention. The convention holds the whole
configuration that filtering needs to create filter types and to translate it to the database. 
During schema creation, the schema builder asks, the convention how the schema should look like. 
The convention defines the names and descriptions of types and fields and also what the type should be used for properties
The convention also defines what provider should be used to translate a GraphQL query to a database query. 
The provider is the only thing that is used after the schema is built. 
Every field or operation in a filter type has a handler annotated.  
During schema initialization, these handlers are bound, to the GraphQL fields. The provider can specify which handler should be bound to which field. 
During execution, the provider visits the incoming value node and executes the handler on the fields. 
This loose coupling allows defining the provider independently of the convention. 

# Filter Convention
A filter convention is a dotnet class that has to implement the interface `IFilterConvention`.
Instead of writing a convention completely new, it is recommended to extend the base convention `FilterConvention`
This convention is also configurable with a fluent interface, so in most cases you can probably just use the descriptor API.

## Descriptor
Most of the capabilities of the descriptor are already documented under `Fetching Data -> Filtering`.
If you have not done this already, it is now the right time to head over to [Filtering](https://chillicream.com/docs/hotchocolate/fetching-data/filtering) and read the parts about the `FilterConventions`

There are two things on this descriptor that are not documented in `Fetching Data`. 

### Operation
```csharp
    IFilterOperationConventionDescriptor Operation(int operationId);
```
Operations are configured globally. Each operation has a unique identifier. You can find the build-in identifiers in `DefaultFilterOperations`. 
This identifier is used in the `FilterInputType<T>`'s to bind operations on a type. Filter operations can also be configured with a fluent interface.
You can specify the name and the description of the operation. This configuration is applied to all operation fields a `FilterInputType<T>` defines.

```csharp
conventionDescriptor
    .Operation(DefaultFilterOperations.Equals)
    .Name("equals")
    .Description("Compares the value of the input to the value of the field");
```

With this configuration, all equals operations are now no longer names `eq` but `equals` and have a description.

If you want to create your own operations, you have to choose an identifier. 
To make sure to not collide with the framework, choose a number that is higher than 1024. 
If you are a framework developer and want to create an extension for HotChocolate, talk to us. 
We can assign you a range of operations so you do not collide with the operations defined by users.

You will need this identifier later, so it probably makes sense to store it somewhere on a class
```csharp
public static class CustomOperations 
{
    public const int Like = 1025;
}

public static class CustomerFilterConventionExtensions 
{
    public static IFilterConventionDescriptor AddInvariantComparison(
        this IFilterConventionDescriptor conventionDescriptor) => 
        conventionDescriptor
            .Operation(CustomOperations.Like)
            .Name("like");
}
```

You know have defined you very own operation.

To apply this configuration to operations types, you can use the Configure method
```csharp
    conventionDescriptor.Configure<StringOperationInputType>(
        x => x.Operation(CustomOperations.Like))
```

### Provider
```csharp
    IFilterConventionDescriptor Provider<TProvider>()
        where TProvider : class, IFilterProvider;
    IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IFilterProvider;
    IFilterConventionDescriptor Provider(Type provider);
```

On the convention, you can also specify what provider should be used. For now you need just to know
that you can configure the provider here. We will have a closer look at the provider later.

```csharp
conventionDescriptor.Provider<CustomProvider>();
```

## Custom Conventions
Most of the time the descriptor API should satisfy your needs. It is recommended to build extensions
based on the descriptor API, rather than creating a custom convention. 
However, if you want to have full control over naming and type creation, you can also override the methods
you need on the `FilterConvention`. 

You can also override the configure method to have a (probably) familiar API experience.

```csharp
public class CustomConvention : FilterConventio
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        desciptor.AddDefaults();
    }

    public override NameString GetTypeName(Type runtimeType) =>
        base.GetTypeName(runtimeType) + "Suffix";
}
```

# Providers

