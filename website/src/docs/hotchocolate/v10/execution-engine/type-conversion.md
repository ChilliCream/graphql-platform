---
title: Type Conversion
---

**For what do we need the type conversion API on Hot Chocolate?**

Let us have a look at a simple example to answer this question and also to show how this is solved with Hot Chocolate.

Assume we have a mongo database entity representation in c# that looks like the following:

```csharp
public class Message
{
    public ObjectId Id { get; set; }
    public DateTimeOffset Created { get; set; }
    public string Text { get; set; }
}
```

We want the `Id` property to be of the `IdType` in the GraphQL schema. The Hot Chocolate query execution engine does not know how `ObjectId` is serialized or deserialized.

Moreover, `IdType` uses `System.String` as .NET representation of its values.

In order to be able to use `ObjectId` through out our code, we have to explain to the query execution engine how to serialize `ObjectId` to `System.String` and also how to deserialize it.

This can be done in simple cases with two lines of code:

```csharp
TypeConversion.Default.Register<string, ObjectId>(from => ObjectId.Parse(from));
TypeConversion.Default.Register<ObjectId, string>(from => from.ToString());
```

# Dependency Injection Support

You can also add your type converters to the dependency injection. Using dependency injection for the type converters lets you more easily write tests that verify behaviour of your API in various scenarious.

The first thing you have to ensure is that your schema has access to the service provider, which can be done like the following:

```csharp
service.AddGraphQL(sp => SchemaBuilder.New()
  .AddServices(sp)
  ...
  .Create()
```

After this is done converters can be registered like the following:

```csharp
services.AddTypeConverter<string, ObjectId>(from => ObjectId.Parse(from));
```

Moreover, you are able to put your conversion code into a class like the follwing:

```csharp
public class StringObjectIdConverter
    : TypeConverter<string, ObjectId>
{
    public ObjectId Convert(string from) => ObjectId.Parse(from);
}
```

This makes sense if you have more complex code to write to specify your conversion.

The class can also be registered with the dependency injection like the following:

```csharp
services.AddTypeConverter<StringObjectIdConverter>();
```
