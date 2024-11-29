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

```graphql
{
  movies(where: { franchise: { eq: "Star Wars" } }) {
    name
  }
}
```

Fields can also form paths. In the query below there are two _fields_ `genre` and `totalMovieCount` and one operation equals
`eq`

```graphql
{
  movies(where: { genre: { totalMovieCount: { eq: 100 } } }) {
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
configuration that filtering needs to create filter types and to translate them to the database.
During schema creation, the schema builder asks the convention how the schema should look like.
The convention defines the names and descriptions of types and fields and also what the type should be used for properties.
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
If you have not done this already, it is now the right time to head over to [Filtering](/docs/hotchocolate/v13/fetching-data/filtering) and read the parts about the `FilterConventions`

There are two things on this descriptor that are not documented in `Fetching Data`:

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
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
    }

    public override NameString GetTypeName(Type runtimeType) =>
        base.GetTypeName(runtimeType) + "Suffix";
}
```

# Providers

Like the convention, a provider can be configured over a fluent interface.
Every filter field or operation has a specific handler defined. The handler translates the operation to the database.
These handlers are stored on the provider. After the schema is initialized, an interceptor visits the filter types and requests a handler from the provider.
The handler is annotated directly on the field.
The provider translates an incoming query into a database query by traversing an input object and executing the handlers on the fields.

The output of a translation is always some kind of _filter definition_. In case, of `IQueryable` this is an expression.
In case, of MongoDB this is a `FilterDefinition`. Provider, visitor context and handler, operate on and produce this _filter definition_.

To inspect and analyze the input object, the provider uses a visitor.

What a visitor is and how you can write you own visitor is explained [here](/docs/hotchocolate/v13/api-reference/visitors)

Visitors are a powerful yet complex concept, we tried our best to abstract it away.
For most cases, you will not need to create a custom visitor.

## Provider Descriptor

The descriptor of a provider is simple. It only has one method:

```csharp
    IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
        where TFieldHandler : IFilterFieldHandler<TContext>;
```

With this method you can register field handlers on the provider.

## Field Handler

Every field or operation is annotated with an instance of a `FilterFieldHandler<TContext, T>`. When the provider is asked for a handler for a field, it iterates sequentially through the list of existing field handlers and calls the `CanHandle` method.
The first field handler that can handle the field, is annotated on the field.
As the visitor traverses the input object, it calls `TryHandleEnter` as it enters the input field and `TryHandleLeave` as it leaves it.

> A field handler supports constructor injection and is a singleton. Do not store data on the field handler. use the `context` of the visitor for state management.

### CanHandle

```csharp
    bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition);
```

Tests if this field handler can handle a field. If it can handle the field it will be attached to it.

### TryHandleEnter

```csharp
bool TryHandleEnter(
    TContext context,
    IFilterField field,
    ObjectFieldNode node,
    [NotNullWhen(true)] out ISyntaxVisitorAction? action);
```

This method is called when the visitor encounters a field.

- `context` is the context of the visitor
- `field` is the instance of the field that is currently visited
- `node` is the field node of the input object. `node.Value` contains the value of the field.
- `action` If `TryHandleEnter` returns true, the action is used for further processing by the visitor.

### TryHandleLeave

```csharp
bool TryHandleLeave(
    TContext context,
    IFilterField field,
    ObjectFieldNode node,
    [NotNullWhen(true)] out ISyntaxVisitorAction? action);
```

This method is called when the visitor leave the field it previously entered.

- `context` is the context of the visitor
- `field` is the instance of the field that is currently visited
- `node` is the field node of the input object. `node.Value` contains the value of the field.
- `action` If `TryHandleLeave` returns true, the action is used for further processing by the visitor.

## Filter Operation Handlers

There is only one kind of field handler. To make it easier to handle operations, there also exists `FilterOperationHandler<TContext, T>`, a more specific abstraction.
You can override `TryHandleOperation` to handle operations.

## The Context

As the visitor and the field handlers are singletons, a context object is passed along with the traversal of input objects.
Field handlers can push data on this context, to make it available for other handlers further down in the tree.

The context contains `Types`, `Operations`, `Errors` and `Scopes`. It is very provider-specific what data you need to store in the context.
In the case of the `IQueryable` provider, it also contains `RuntimeTypes` and knows if the source is `InMemory` or a database call.

With `Scopes` it is possible to add multiple logical layers to a context. In the case of `IQueryable` this is needed, whenever a new closure starts

```csharp
//          /------------------------ SCOPE 1 -----------------------------\
//                                        /----------- SCOPE 2 -------------\
users.Where(x => x.Company.Addresses.Any(y => y.Street == "221B Baker Street"))
```

A filter statement that produces the expression above would look like this

```graphql
{
  users(
    where: {
      company: { addresses: { any: { street: { eq: "221B Baker Street" } } } }
    }
  ) {
    name
  }
}
```

A little simplified this is what happens during visitation:

```graphql
{
  users(
    # level[0] = []
    # instance[0] = x
    # Create SCOPE 1 with parameter x of type User
    where: {
      # Push property User.Company onto the scope
      # instance[1] =  x.Company
      # level[1] = []
      company: {
        # Push property Company.Addresses onto the scope
        # instance[2] x.Company.Addresses
        # level[2] = []
        addresses: {
          # Create SCOPE 2 with parameter y of type Address
          # instance[0] = y
          # level[0] = []
          any: {
            # Push property Address.Street onto the scope
            # instance[1] = y.Street
            # level[1] = []
            street: {
              # Create and push the operation onto the scope
              # instance[2] = y.Street
              # level[2] = [y.Street == "221B Baker Street"]
              eq: "221B Baker Street"
            }
            # Combine everything of the current level and pop the property street from the instance
            # instance[1] = y.Street
            # level[1] = [y.Street == "221B Baker Street"]
          }
          # Combine everything of the current level, create the any operation and exit SCOPE 2
          # instance[2] = x.Company.Addresses
          # level[2] = [x.Company.Addresses.Any(y => y.Street == "221B Baker Street")]
        }
        # Combine everything of the current level and pop the property street from the instance
        # instance[1] = x.Company
        # level[1] = [x.Company.Addresses.Any(y => y.Street == "221B Baker Street")]
      }
      # Combine everything of the current level and pop the property street from the instance
      # instance[0] = x
      # level[0] = [x.Company.Addresses.Any(y => y.Street == "221B Baker Street")]
    }
  ) {
    name
  }
}
```

# Extending IQueryable

The default filtering implementation uses `IQueryable` under the hood. You can customize the translation of queries by registering handlers on the `QueryableFilterProvider`.

The following example creates a `StringOperationHandler` that supports case insensitive filtering:

```csharp
// The QueryableStringOperationHandler already has an implementation of CanHandle
// It checks if the field is declared in a string operation type and also checks if
// the operation of this field uses the `Operation` specified in the override property further
// below
public class QueryableStringInvariantEqualsHandler : QueryableStringOperationHandler
{
    public QueryableStringInvariantEqualsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    // For creating a expression tree we need the `MethodInfo` of the `ToLower` method of string
    private static readonly MethodInfo _toLower = typeof(string)
        .GetMethods()
        .Single(
            x => x.Name == nameof(string.ToLower) &&
            x.GetParameters().Length == 0);

    // This is used to match the handler to all `eq` fields
    protected override int Operation => DefaultFilterOperations.Equals;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object parsedValue)
    {
        // We get the instance of the context. This is the expression path to the property
        // e.g. ~> y.Street
        Expression property = context.GetInstance();

        // the parsed value is what was specified in the query
        // e.g. ~> eq: "221B Baker Street"
        if (parsedValue is string str)
        {
            // Creates and returns the operation
            // e.g. ~> y.Street.ToLower() == "221b baker street"
            return Expression.Equal(
                Expression.Call(property, _toLower),
                Expression.Constant(str.ToLower()));
        }

        // Something went wrong 😱
        throw new InvalidOperationException();
    }
}
```

This operation handler can be registered on the convention:

```csharp
public class CustomFilteringConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.Provider(
            new QueryableFilterProvider(
                x => x
                    .AddDefaultFieldHandlers()
                    .AddFieldHandler<QueryableStringInvariantEqualsHandler>()));
    }
}

// and then
services.AddGraphQLServer()
    .AddFiltering<CustomFilteringConvention>();
```

To make this registration easier, Hot Chocolate also supports convention and provider extensions.
Instead of creating a custom `FilterConvention`, you can also do the following:

```csharp
services
    .AddGraphQLServer()
    .AddFiltering()
    .AddConvention<IFilterConvention>(
        new FilterConventionExtension(
            x => x.AddProviderExtension(
                new QueryableFilterProviderExtension(
                    y => y.AddFieldHandler<QueryableStringInvariantEqualsHandler>()))));
```
