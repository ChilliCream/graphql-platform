---
title: Star Wars
---

We have created the Star Wars sample in different versions in order to show different ways to build your GraphQL server off.

# Pure Code-First

Pure Code-First is our newest variant to build GraphQL by just using clean C#. You really do not need to deal with schema types directly and you still get all the power that you would have with schema types. You can split types use field middleware and all of this without any clutter.

```csharp
public class Query
{
    /// <summary>
    /// Gets all character.
    /// </summary>
    /// <param name="repository"></param>
    /// <returns>The character.</returns>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IEnumerable<ICharacter> GetCharacters(
        [Service]ICharacterRepository repository) =>
        repository.GetCharacters();
}
```

[Star Wars - Pure Code-First](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/PureCodeFirst)

We have also created a dotnet CLI template for the pure Code-First Star Wars example. You can follow the example and get an impression how you could implement such a schema with our API.

```bash
dotnet new -i HotChocolate.Templates.StarWars
```

After you have installed this template you can just fire up the following command in order to get the example up and running:

```bash
mkdir starwars
cd starwars
dotnet new starwars
dotnet run --project StarWars/StarWars.csproj -c release
```

The service should start-up and run on the port 5000. In order to test your server and send queries head over to our playground endpoint: `http://127.0.0.1:5000/graphql/playground`

> Note: The port may vary depending on if you start this project from the console with `dotnet run` or from Visual Studio.

Try a query like the following to get started:

```graphql
{
  character(ids: 1000) {
    name
    appearsIn
    friends {
      nodes {
        name
        appearsIn
      }
    }
  }
}
```

The template source code is located [here](https://github.com/ChilliCream/hotchocolate/tree/master/examples).

# Code-First

Code-First or Code-First with schema types lets you use our fluent type API to describe your schema types. The resolver code and the actual types are disjunct you explicitly can express your schema types without even needing a C# backing type.

```csharp
public class QueryType : ObjectType<Query>
{
   protected override Configure(IObjectTypeDescriptor<Query> descriptor)
   {
      descriptor.Field(t => t.GetCharacters(default))
         .UsePaging<NonNullType<CharacterType>>()
         .UseFiltering()
         .UseSorting();
   }
}

public class Query
{
    /// <summary>
    /// Gets all character.
    /// </summary>
    /// <param name="repository"></param>
    /// <returns>The character.</returns>
    public IEnumerable<ICharacter> GetCharacters(
        [Service]ICharacterRepository repository) =>
        repository.GetCharacters();
}
```

[Star Wars - Code-First](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/CodeFirst)

# Schema-First

We are currently working on a schema-first example. Example Coming Soon :)
