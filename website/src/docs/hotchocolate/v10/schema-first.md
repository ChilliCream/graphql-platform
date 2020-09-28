---
title: Schema-first
---

GraphQL has an easy and beautiful syntax to describe schemas.

Hot Chocolate supports the GraphQL schema definition language and lets you easily bind resolvers or types to the defined schema.

OK, let us get started by defining a simple hello world schema:

```sdl
type Query {
  hello: String
}
```

In order to host the above schema we have to use the SchemaBuilder to load it and then bind a resolver to the `hello` field.

```csharp
var schema = SchemaBuilder.New()
    .AddDocumentFromString(
        @"
        type Query {
            hello: String
        }")
    .AddResolver("Query", "hello", () => "world")
    .Create();

var executor = schema.MakeExecutable();

Console.WriteLine(executor.Execute("{ hello }").ToJson());
```

If you have larger schemas it may be not feasible for you to define resolvers for all of your fields.

The easier way in these cases is to write classes that contain your resolvers and bind those to the various types.

```csharp
var schema = SchemaBuilder.New()
    .AddDocumentFromString(
        @"
        type Query {
            hello: String
        }")
    .BindComplexType<Query>()
    .Create();

public class Query
{
    public string GetHello() => "world";
}
```

Sometimes you have business objects that already exist and that do not exactly match your schema like in the above example.

In these cases you can easily tell the `SchemaBuilder` how it can map your business object to the schema type.

```csharp
var schema = SchemaBuilder.New()
    .AddDocumentFromString(
        @"
        type Query {
            hello: String
        }")
    .BindComplexType<Query>(c => c.Field(t => t.GetGreetings()).Name("hello"))
    .Create();

public class Query
{
    public string GetGreetings() => "world";
}
```

Furthermore, we can add new functionality on top of our business objects without changing them. This is done by adding a new type that basically contains only resolvers. Hot Chocolate differentiates between the data objects and the resolver objects and lets you bind both to the schema.

```csharp
public class QueryResolvers
{
    public string GetHello([Parent]Query query) => query.GetGreetings();
}
```

Our schema would now be declared like the following:

```csharp
var schema = SchemaBuilder.New()
    .AddDocumentFromString(
        @"
        type Query {
            hello: String
        }")
    .BindComplexType<Query>(c => c.Field(t => t.GetGreetings()))
    .BindResolver<QueryResolvers>(c => c.To<Query>())
    .Create();
```

You can also mix and match resolvers binding, type bindings, delegate resolver or even bring in some code-first:

```csharp
var schema = SchemaBuilder.New()
    .AddDocumentFromString(
        @"
        type Query {
            hello: String
            foo: Bar
        }")
    .BindComplexType<Query>(c => c.Field(t => t.GetGreetings()))
    .BindResolver<QueryResolvers>(c => c.To<Query>())
    .AddResolver("Query", "foo", () => new Bar())
    .AddType<BarType>()
    .Create();
```
