---
id: resolvers
title: Resolvers
---

In GraphQL resolvers describe the logic that fetches data for a specific field.

Field resolvers run independently of each other which allows the execution engine to parallelize their execution.

This independent approach to fetch data allows us to build powerful schemas that consists of multiple data sources in a very simple way.

Since we have two major approaches with _Hot Chocolate_ to define a schema, we also have two approaches to declaring our resolvers. We will start by looking at how we can declare resolvers with the schema-first approach and then look at how this is done in the code-first world.

It is important to know that we can mix both approaches. Moreover, resolvers are integrated as a component into the field-middleware-pipeline. A field-middleware is more complex but can also open up more scenarios. One could for instance write a middleware that resolves the data for multiple fields of a certain well-defined data source.

> More about what a field-middleware can do can be found [here](middleware.md).

## Schema-First

With _Hot Chocolate_ we have multiple approaches to write resolvers depending on how you declare your schema.

With the schema-first approach the simplest way to declare a resolver is binding a delegate that resolves the data to a field in your schema like the following:

```csharp
c.BindResolver(ctx =>
{
    // my resolver code goes here ...
}).To("Query", "foo");
```

Furthermore, we could bind a class as a resolver type. Each of the members of the resolver type can be bound to fields in the schema.

```csharp
services.AddGraphQL(so =>
    SchemaBuilder.New()
        .AddDocumentFromFile("schema.graphql")
        .BindComplexType<Query>(b => b
            .To("Query")
            .Field(t => t.GetGreetings())
            .Name("greetings"))
        .Create());
```

Since, the class `Query` is used as our resolver type, the query engine will automatically create an instance of this type as singleton. The lifetime of the resolver object is basically bound to the lifetime of the query executor.

We can also take charge of the lifetime management by registering the resolver type with the dependency injection. In this case the query engine will retrieve the type from the `IServiceProvider` and not perform any lifetime management.

Sometimes, we do not want to explicitly declare resolvers since we have already modeled our entities very well and just want to map those to our schema. In this case we can just bind our type like the following:

```csharp
services.AddGraphQL(so =>
    SchemaBuilder.New()
        .AddDocumentFromFile("schema.graphql")
        .BindComplexType<Query>(b => b.To("Query"))
        .Create());
```

Entities are handled differently than resolver types.

First of all you are able to pass in an entity object on which the resolvers are executed. In this case the query engine will do nothing and operate on the passed in entity.

If no initial root value was passed into the query engine, the query engine will create a new `Query` object by itself. The instance will be disposed (if disposable) after the request was completed.

Like with the resolver type we can take charge of the lifetime by registering the root types as services with our dependency injection.

In the case that we have not specified any resolvers for our bound entity, _Hot Chocolate_ will generate an in-memory assembly that contains the inferred resolvers. 

Moreover, we can combine our approach in order to provide specific resolver logic for our entity or in order to extend on our entity. In many cases our entity may just represent part of the data structure that we want to expose in our schema. In this case we can just provide resolver logic to fill the gaps.

```csharp
services.AddGraphQL(so =>
    SchemaBuilder.New()
        .AddDocumentFromFile("schema.graphql")
        .BindComplexType<Query>(b => b.To("Query"))
        .BindResolver<QueryResolver>(b => b
            .To("Query")
            .Resolve("greetings")
            .With(t => t.GetGreetings(default)))
        .Create());
```

In the above case the `GetGreetings` method has an argument `Query` which is our bound entity. Resolver methods can specify the original field arguments as specified by the field definition as well as context arguments.

```csharp
public string GetGreetings([Parent]Query query) => query.Greetings;
```

The `ParentAttribute` signals the query engine that this argument shall be the instance of the declaring type of our field.

We also could let the query engine inject us the resolver context which provides us with all the context data for our resolver.

For example we could access all the previous resolved object in our path by accessing `IResolverContext.Source`. Or, we could access scoped context data that were passed down by one of the previous resolvers in the path.

In order to keep our resolver clean and easy to test we can also just let the query engine inject the parts of the resolver context that we really need for our resolver like the `ObjectType` to which our current field belongs etc.

```csharp
public string GetGreetings(ObjectType type) => type.Name;
```

## Code-First

Code-first is the second approach with which we can be describe a GraphQL schema. In Code-first, field definition and resolver logic are more closely bound together.

```csharp
public class QueryType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");
        descriptor.Field("greetings").Resolver(ctx => "foo");
    }
}
```

The above example declares a schema type named `Query` with one field called `greetings` of the type `String` that always returns `foo`. Like with the schema-first approach we can create types that are not explicitly bound to a specific .NET type like in the above example.

```csharp
public class PersonType
    : ObjectType<Person>
{
// Types inferred
}
```

If we bind our type to a specific entity type using `ObjectType<T>`, then we will by default infer the possible type structure and its resolvers from the .NET type.

We can always overwrite the defaults or define everything explicitly.

```csharp
public class PersonType
    : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Name("Person123");
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.FriendId)
            .Name("friend")
            .Resolver(ctx => ctx.Service<IRepository>().GetPerson(ctx.Parent<Person>().FriendId));
    }
}
```

## Resolver Types

Since, a lot of resolver logic, like the one in the above example, can be difficult to test and difficult to read, we also allow to create resolver types with the code-first approach.

We can explicitly include resolvers from a resolver type the same way we are specifying our fields.

```csharp
descriptor.Field<PersonResolvers>(t => t.GetFriend(defaults)).Type<PersonType>();
```

The one difference is that we basically specify from which type we are including the resolver.

Furthermore, we can also include all fields of a resolver type implicitly like the following:

```csharp
descriptor.Include<PersonResolvers>();
```

We can also reverse the relationship between the type and its resolvers by annotating the resolver type with the entity or schema type name for which the resolver type provides resolvers.

```csharp
[GraphQLResolverOf(typeof(Person))]
[GraphQLResolverOf("Query")]
public class SomeResolvers
{
    public Person GetFriend([Parent]Person person)
    {
        // resolver code
    }

    [GraphQLDescription("This field does ...")]
    public string GetGreetings([Parent]Query person, string name)
    {
        // resolver code
    }
}
```

The above example class `SomeResolvers` provides resolvers for multiple types. The types can be declared with the `GraphQLResolverOfAttribute` either by providing the .NET entity type or by providing the schema type name. This resolver can be registered with the schema builder via `BindResolver<SomeResolvers>()` as shown here:
```csharp
services.AddGraphQL(so =>
    SchemaBuilder.New()
        // ...
        .BindResolver<SomeResolvers>()
        // ...
        .Create());
```

The schema builder will associate the various resolver methods with the correct schema fields and types by analysing the method parameters. We are providing a couple of attributes that can be used to give the resolver method more context like the return type or the description and so on.

## Resolver Dependency Injection

Hot Chocolate supports resolver parameter dependency injection. Basically we are able to inject things that we would usually get from the resolver context itself. This makes it clear what demands the resolver has.

Let us have a look at an example:

```csharp
public Person GetFriend([Parent]Person person, IObjectField field)
{
    // resolver code
}
```

The above resolver is injected with the previously resolved (parent) result and the field definition of the current field.

Any property of the resolver context can be explicitly injected as argument.

The following resolver context properties can be injected without any attributes:

| Member                     | Type                      | Description                                                       |
| -------------------------- | ------------------------- | ----------------------------------------------------------------- |
| `Schema`                   | `ISchema`                 | The GraphQL schema.                                               |
| `ObjectType`               | `ObjectType`              | The object type on which the field resolver is being executed.    |
| `Field`                    | `ObjectField`             | The field on which the field resolver is being executed.          |
| `QueryDocument`            | `DocumentNode`            | The query that is being executed.                                 |
| `Operation`                | `OperationDefinitionNode` | The operation from the query that is being executed.              |
| `FieldSelection`           | `FieldNode`               | The field selection for which a field resolver is being executed. |
| `Path`                     | `Path`                    | The current execution path.                                       |
| `Argument<T>(string name)` | `T`                       | Gets a specific field argument.                                   |

The following resolver context data can be accessed by annotating the method argument with an attribute.

### Parent

```csharp
public Person GetFriend([Parent]Person person)
{
    // resolver code
}
```

### Services

```csharp
public Person GetFriend([Service]IPersonRepository repository)
{
    // resolver code
}
```


### DataLoader

```csharp
public Person GetFriend([DataLoader]IPersonDataLoader dataLoader)
{
    // resolver code
}
```

Or

```csharp
public Person GetFriend([DataLoader("ById")]IPersonDataLoader dataLoader)
{
    // resolver code
}
```

### State

```csharp
public Person GetFriend([State("foo")]Bar bar)
{
    // resolver code
}
```

## Resolver Context Overview

| Member                      | Type                      | Description                                                                           |
| --------------------------- | ------------------------- | ------------------------------------------------------------------------------------- |
| `Schema`                    | `ISchema`                 | The GraphQL schema.                                                                   |
| `ObjectType`                | `ObjectType`              | The object type on which the field resolver is being executed.                        |
| `Field`                     | `ObjectField`             | The field on which the field resolver is being executed.                              |
| `QueryDocument`             | `DocumentNode`            | The query that is being executed.                                                     |
| `Operation`                 | `OperationDefinitionNode` | The operation from the query that is being executed.                                  |
| `FieldSelection`            | `FieldNode`               | The field selection for which a field resolver is being executed.                     |
| `Path`                      | `Path`                    | The current execution path.                                                           |
| `Argument<T>(string name)`  | `T`                       | Gets a specific field argument.                                                       |
| `Source`                    | `ImmutableStack<object>`  | The source stack contains all previous resolver results of the current execution path |
| `Parent<T>()`               | `T`                       | Gets the previous (parent) resolved result.                                           |
| `Service<T>()`              | `T`                       | Gets as specific service from the dependency injection container.                     |
| `CustomContext<T>()`        | `T`                       | Gets a specific custom context object that can be used to build up a state.           |
| `DataLoader<T>(string key)` | `T`                       | Gets a specific DataLoader.                                                           |
