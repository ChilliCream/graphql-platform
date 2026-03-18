---
title: Extending Filtering
description: Learn how to extend the filtering system in Hot Chocolate v16 with custom conventions, providers, and field handlers.
---

The `HotChocolate.Data` package works with all databases that support `IQueryable`. The default settings include all filter operations that work over `IQueryable` on all databases. In some cases, this is not enough. Some databases might not support `IQueryable`. Others may have technology-specific operations (e.g., SQL `LIKE`). The filtering system is designed with extensibility in mind.

Filtering can be broken down into two parts: schema building and execution. During schema building, filter input types are created. During execution, user-provided data is analyzed and translated to a database query. Both parts are configured through a convention.

You are free to design the structure of filters as it suits you best. Typically, you divide the structure into two parts: the _field_ and the _operation_.

The query below returns all movies where the franchise equals "Star Wars". The _field_ is `franchise` and the _operation_ is equals (`eq`):

```graphql
{
  movies(where: { franchise: { eq: "Star Wars" } }) {
    name
  }
}
```

Fields can form paths. The following query has two _fields_ (`genre` and `totalMovieCount`) and one operation (`eq`):

```graphql
{
  movies(where: { genre: { totalMovieCount: { eq: 100 } } }) {
    name
  }
}
```

A field is always context-specific. Even when two fields share the same name (like `description` on a movie and `description` on a genre), they have different meanings. An operation, on the other hand, always has the same meaning. The equals operation (`eq`) always means the field value should equal the provided value. Operations can be applied in different contexts, but the operation itself stays the same. There should be only one operation that checks for equality, and it should always have the same name.

# How Everything Fits Together

At the core of the configuration API sits a convention. The convention holds the entire configuration that filtering needs to create filter types and translate them to the database.

During schema creation, the schema builder asks the convention how the schema should look. The convention defines the names, descriptions, and types used for properties. The convention also defines which provider should translate a GraphQL query to a database query.

The provider is the only component used after the schema is built. Every field or operation in a filter type has a handler annotated. During schema initialization, these handlers are bound to the GraphQL fields. The provider specifies which handler should be bound to which field. During execution, the provider visits the incoming value node and executes the handler on the fields.

This loose coupling allows defining the provider independently of the convention.

# Filter Convention

A filter convention is a .NET class that implements `IFilterConvention`. Instead of writing a convention from scratch, extend the `FilterConvention` base class. This convention is configurable through a fluent interface, so in most cases you can use the descriptor API.

## Descriptor

Most descriptor capabilities are documented under [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering). Read the parts about `FilterConventions` there first.

Two features on the descriptor are specific to extensibility:

### Operation

```csharp
IFilterOperationConventionDescriptor Operation(int operationId);
```

Operations are configured globally. Each operation has a unique identifier. You can find the built-in identifiers in `DefaultFilterOperations`. This identifier is used in `FilterInputType<T>` to bind operations on a type. Filter operations are configurable through a fluent interface where you specify the name and description. This configuration applies to all operation fields across all `FilterInputType<T>` definitions.

```csharp
conventionDescriptor
    .Operation(DefaultFilterOperations.Equals)
    .Name("equals")
    .Description("Compares the value of the input to the value of the field");
```

With this configuration, all equals operations are now named `equals` (instead of `eq`) and have a description.

To create your own operations, choose an identifier higher than 1024 to avoid collisions with the framework. Store the identifier on a class for reference:

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

To apply this configuration to operation types, use the `Configure` method:

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

You configure the provider on the convention. More details on providers appear later in this page.

```csharp
conventionDescriptor.Provider<CustomProvider>();
```

## Custom Conventions

Most of the time the descriptor API should satisfy your needs. Building extensions based on the descriptor API is recommended over creating a custom convention. However, if you need full control over naming and type creation, override the methods on `FilterConvention`:

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

Like the convention, a provider is configured through a fluent interface. Every filter field or operation has a specific handler. The handler translates the operation to the database. Handlers are stored on the provider. After schema initialization, an interceptor visits the filter types and requests a handler from the provider, which is then annotated on the field.

The provider translates an incoming query into a database query by traversing an input object and executing the handlers on the fields. The output is always some kind of _filter definition_. For `IQueryable`, this is an expression. For MongoDB, this is a `FilterDefinition`.

To inspect and analyze the input object, the provider uses a visitor. See [Visitors](/docs/hotchocolate/v16/api-reference/visitors) for details on how visitors work.

## Provider Descriptor

The provider descriptor has one method:

```csharp
IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
    where TFieldHandler : IFilterFieldHandler<TContext>;
```

Use this method to register field handlers on the provider.

## Field Handler

Every field or operation is annotated with an instance of `FilterFieldHandler<TContext, T>`. When the provider looks for a handler for a field, it iterates through the list of registered handlers and calls `CanHandle`. The first handler that can handle the field is annotated on it. During visitation, the visitor calls `TryHandleEnter` when it enters an input field and `TryHandleLeave` when it leaves.

> A field handler supports constructor injection and is a singleton. Do not store state on the field handler. Use the visitor context for state management.

### CanHandle

```csharp
bool CanHandle(
    ITypeCompletionContext context,
    IFilterInputTypeDefinition typeDefinition,
    IFilterFieldDefinition fieldDefinition);
```

Tests whether this handler can handle a field. If it can, it is attached to the field.

### TryHandleEnter

```csharp
bool TryHandleEnter(
    TContext context,
    IFilterField field,
    ObjectFieldNode node,
    [NotNullWhen(true)] out ISyntaxVisitorAction? action);
```

Called when the visitor encounters a field. The parameters are:

- `context` -- the visitor context
- `field` -- the field instance being visited
- `node` -- the field node from the input object (`node.Value` contains the value)
- `action` -- if the method returns `true`, this action controls further visitor processing

### TryHandleLeave

```csharp
bool TryHandleLeave(
    TContext context,
    IFilterField field,
    ObjectFieldNode node,
    [NotNullWhen(true)] out ISyntaxVisitorAction? action);
```

Called when the visitor leaves the field it previously entered.

## Filter Operation Handlers

`FilterOperationHandler<TContext, T>` is a more specific abstraction for handling operations. Override `TryHandleOperation` to handle operations.

## The Context

The visitor and field handlers are singletons, so a context object is passed along during traversal. Handlers can push data onto this context for other handlers further down the tree.

The context contains `Types`, `Operations`, `Errors`, and `Scopes`. What data you store is provider-specific. The `IQueryable` provider also contains `RuntimeTypes` and knows whether the source is `InMemory` or a database call.

`Scopes` allow adding multiple logical layers to a context. For `IQueryable`, this is needed whenever a new closure starts:

```csharp
//          /------------------------ SCOPE 1 -----------------------------\
//                                        /----------- SCOPE 2 -------------\
users.Where(x => x.Company.Addresses.Any(y => y.Street == "221B Baker Street"))
```

# Extending IQueryable

The default filtering implementation uses `IQueryable` under the hood. You can customize query translation by registering handlers on the `QueryableFilterProvider`.

The following example creates a `StringOperationHandler` that supports case-insensitive filtering:

```csharp
public class QueryableStringInvariantEqualsHandler : QueryableStringOperationHandler
{
    public QueryableStringInvariantEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    private static readonly MethodInfo s_toLower = typeof(string)
        .GetMethods()
        .Single(
            x => x.Name == nameof(string.ToLower) &&
            x.GetParameters().Length == 0);

    protected override int Operation => DefaultFilterOperations.Equals;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object parsedValue)
    {
        Expression property = context.GetInstance();

        if (parsedValue is string str)
        {
            return Expression.Equal(
                Expression.Call(property, s_toLower),
                Expression.Constant(str.ToLower()));
        }

        throw new InvalidOperationException();
    }
}
```

Register this handler on a custom convention:

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

builder.Services
    .AddGraphQLServer()
    .AddFiltering<CustomFilteringConvention>();
```

You can also use convention and provider extensions instead of creating a custom `FilterConvention`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddFiltering()
    .AddConvention<IFilterConvention>(
        new FilterConventionExtension(
            x => x.AddProviderExtension(
                new QueryableFilterProviderExtension(
                    y => y.AddFieldHandler<QueryableStringInvariantEqualsHandler>()))));
```

# Troubleshooting

**Custom handler is not invoked**
Verify that your handler's `CanHandle` method returns `true` for the expected field. Handlers are checked in registration order, and the first match wins.

**Filter operations not appearing in the schema**
Confirm that you registered the operation on the convention descriptor and that the operation is applied to the correct `FilterInputType<T>` using the `Configure` method.

**"Provider not found" errors**
Ensure that the convention and provider are registered on the schema builder. If you are using scoped conventions, verify the scope matches between the registration and the attribute.

# Next Steps

- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) for using built-in filtering
- [Visitors](/docs/hotchocolate/v16/api-reference/visitors) for understanding the visitor pattern
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb) for MongoDB-specific filtering
