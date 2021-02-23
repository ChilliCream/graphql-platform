---
path: "/blog/2019/09/27/strawberry-shake"
date: "2019-09-27"
title: "Building a .NET GraphQL Client API"
featuredImage: "shared/strawberry-shake-banner.png"
tags: ["strawberry-shake", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

**This post has been updated, please head over to the newer post [here](https://chillicream.com/blog/2019/11/25/strawberry-shake_2).**

We for a while now have two big GraphQL server projects on the .NET platform. So, if you just want to build a decent GraphQL server you can pick and choose between _GraphQL .NET_ or Hot Chocolate.

If you are looking at consuming a GraphQL server in your _Blazor_ or _Xamarin_ application, then things are not so promising. You can either go with a bare bone client from the _GraphQL .NET_ project or you can decide to go it alone and build on `HttpClient`.

After the version 10 release of our Hot Chocolate GraphQL server we have started to build a new GraphQL client API that is more in line with how people in JavaScript consume GraphQL endpoints.

## Introduction

Before we get into it let me first outline what our goals for our approach are:

- Strongly typed API.
- Define the API with GraphQL.
- No magic strings.
- Everything compiles.
- Customizable request pipelines.
- Support for local resolvers.
- Support for custom scalars.

The preview that we released today is a prototype that has a ton of bugs and is meant at the moment to get feedback. Starting with this preview we will now release every other day a new preview and think that we will release this new API with version 11 of Hot Chocolate.

## Getting Started

Let us have a look at how we want to tackle things with _Strawberry Shake_. For this little walkthrough I will use our [_Star Wars_ server example](https://github.com/ChilliCream/hotchocolate/tree/master/examples/AspNetCore.StarWars).

If you want to follow along then install the [.NET Core 3 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) . We are also supporting other .NET variants but for this example you will need the .NET Core 3 SDK.

Before we can start let us clone the Hot Chocolate repository and start our _Star Wars_ server.

```bash
git clone https://github.com/ChilliCream/hotchocolate.git
cd hotchocolate
dotnet run --project examples/AspNetCore.StarWars/
```

Now that we have our _Star Wars_ server running, lets create a folder for our client and install the _Strawberry Shake_ tools.

```bash
mkdir berry
dotnet new tool-manifest
dotnet tool install StrawberryShake.Tools --version 11.0.0-preview.35 --local
```

In our example we are using the new .NET CLI local tools. `dotnet new tool-manifest` creates the tools manifest which basically is like a packages.config and holds the information of which tools in which version we are using.

The next command `dotnet tool install StrawberryShake.Tools --version 11.0.0-preview.35 --local` installs our _Strawberry Shake_ tools.

Next we need a little project. Let’s create a new console application so that we can easily run and debug what we are doing.

```bash
dotnet new console -n BerryClient
cd BerryClient
dotnet add package StrawberryShake --version 11.0.0-preview.35
dotnet add package Microsoft.Extensions.Http --version 3.0.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 3.0.0
```

OK, now that we have a project setup lets initialize the project by creating a local schema. Like with _relay_ we are holding a local schema file that can be extended with local types and fields. Our _Graphql_ compiler will use this schema information to validate the queries.

> For the next step ensure that the _Star Wars_ _GraphQL_ server is running since we will fetch the schema from the server.

```bash
dotnet graphql init ./StarWars http://localhost:5000/graphql -n StarWars
```

The init command will download the schema as GraphQL SDL and create a config to refetch the schema. Also, the config contains the client name. The client name defines how the client class is and interface is named.

> Note: You can pass in the token and scheme if your endpoint is authenticated. There is also an update command to update the local schema.

The configuration will look like the following:

```json
{
  "Schemas": [
    {
      "Name": "StarWars",
      "Type": "http",
      "File": "StarWars.graphql",
      "Url": "http://localhost:5000/graphql"
    }
  ],
  "ClientName": "StarWarsClient"
}
```

OK, now let’s get started by creating our first client API. For this open your editor of choice. I can recommend using VSCode for this at the moment since you will get GraphQL highlighting. As we move forward, we will refine the tooling more and provide proper IntelliSense.

Now let us create a new file in our `StarWars` folder called `Queries.graphql` and add the following query:

```graphql
query getFoo {
  foo
}
```

Now build your project.

```bash
dotnet build
```

When we now compile, we get an _MSBuild_ error on which we can click in VSCode and we are pointed to the place in our query file from which the error stems from. The error tells us that there is no field `foo` on the `Query` type.

```bash
/Users/michael/Local/play/berry/BerryClient/StarWars/Queries.graphql(2,3): error GQL: The field `foo` does not exist on the type `Query`. [/Users/michael/Local/play/berry/BerryClient/BerryClient.csproj]
```

Your GraphQL query document is not just a string, it properly compiles and is fully typed. Let's change our query to the following and compile again:

```graphql
query getFoo {
  hero {
    name
  }
}
```

```bash
dotnet build
```

Now our project changes and we get a new `Generated` folder that has all the types that we need to communicate with our backend.

Let us have a look at our client interface for a minute.

```csharp
public interface IStarWarsClient
{
    Task<IOperationResult<IGetFoo>> GetFooAsync();

    Task<IOperationResult<IGetFoo>> GetFooAsync(
        CancellationToken cancellationToken);
}
```

The client will have for each operation in your query file one method that will execute that exact operation.

Since, with GraphQL you essentially design your own service API by writing a query document your types can become quite messy very quickly.

In order to avoid getting a messy API and to give you control over how your C# API will look like we are using fragments to infer types.

Let us redesign our query with fragments and make it a bit more complex.

```graphql
query getHero {
  hero {
    ...SomeDroid
    ...SomeHuman
  }
}

fragment SomeHuman on Human {
  ...HasName
  homePlanet
}

fragment SomeDroid on Droid {
  ...HasName
  primaryFunction
}

fragment HasName on Character {
  name
}
```

The fragments will yield in the following type structure:

```csharp
public interface ISomeHuman
    : IHasName
{
    string HomePlanet { get; }
}

public interface ISomeDroid
    : IHasName
{
    string PrimaryFunction { get; }
}

public interface IHasName
{
    string Name { get; }
}
```

As we go forward, we will introduce some directives that will let you further manipulate the types like `@spread`. `@spread` will let you spread the fields of a child object over its parent object.

Let's make one more tweak to our query and then we get this example running.

```graphql
query getHero($episode: Episode) {
  hero(episode: $episode) {
    ...SomeDroid
    ...SomeHuman
  }
}

fragment SomeHuman on Human {
  ...HasName
  homePlanet
}

fragment SomeDroid on Droid {
  ...HasName
  primaryFunction
}

fragment HasName on Character {
  name
}
```

By definig a variable with our operation we now can pass in arguments into our operation.

```csharp
public interface IStarWarsClient
{
    Task<IOperationResult<IGetHero>> GetHeroAsync(
        Episode episode);

    Task<IOperationResult<IGetHero>> GetHeroAsync(
        Episode episode,
        CancellationToken cancellationToken);
}
```

OK, let's get it running and then go into more details. By default the generator will also generate dependency injection code for `Microsoft.Extensions.DependencyInjection`. In order to get our client up and running we just have to set up a dependency injection container.

> Note: You can shut of dependency injection generation with a _MSBuild_ property. The client can also be instantiated with a builder or by using a different dependency injection container.

Replace you `Program` class with the following code.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDefaultScalarSerializers();
        serviceCollection.AddStarWarsClient();
        serviceCollection.AddHttpClient("StarWarsClient")
            .ConfigureHttpClient(client =>
                client.BaseAddress = new Uri("http://localhost:5000/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();

        IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Newhope);
        Console.WriteLine(((ISomeDroid)result.Data.Hero).Name);

        result = await client.GetHeroAsync(Episode.Empire);
        Console.WriteLine(((ISomeHuman)result.Data.Hero).Name);
    }
}
```

Run the console and it will output the following;

```bash
R2-D2
Luke Skywalker
```

## Generation Options

By default, _Strawberry Shake_ will generate C# 7.3 without nullable reference types. We also by default generate dependency injection code for `Microsoft.Extensions.DependencyInjection`.

If the generator detects that you are using C# 8.0 and enabled support for nullable reference types, then the generate is switching to produce code with nullable reference types.

```xml
<PropertyGroup>
  <LangVersion>8.0</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

In order to manually overwrite those defaults, we added some build properties that you can use.

```xml
<PropertyGroup>
  <BerryLangVersion>CSharp_8_0</BerryLangVersion>
  <BerryEnableDI>true</BerryEnableDI>
  <BerryNamespace>$(RootNamespace)</BerryNamespace>
</PropertyGroup>
```

## Dependency Injection

The client API can be used with other dependency injection container and also without dependency injection at all.

The execution pipeline can be extended or completely swapped out. This is an important aspect of _Strawberry Shake_ since this allows us to add cross-cutting concerns like auto-batching, persisted query support and other features.

```csharp
private static IServiceCollection TryAddDefaultHttpPipeline(
    this IServiceCollection serviceCollection)
{
    serviceCollection.TryAddSingleton<OperationDelegate>(
        sp => HttpPipelineBuilder.New()
            .Use<CreateStandardRequestMiddleware>()
            .Use<SendHttpRequestMiddleware>()
            .Use<ParseSingleResultMiddleware>()
            .Build(sp));
    return serviceCollection;
}
```

When used with Microsoft's dependency injection container then we are also using the `IHttpFactory` which allows for integration with polly and others.

## Roadmap

We are still heavy at work on the client and generator and this first preview is where we invite people to try it out in order to get feedback.

There is still a ton of work to be done and a ton of tests to be written to get this up for prime time.

We will have I think two more weeks to work on the generator to iron out generation issues. We will add documentation tags and things like that over the next view weeks.

Also, there are some generator directives that should show up next week like `@spread`, `@name` and `@type`.

Moreover, we will add support for local schema stitching. We already integrated the stitching engine into the generator but have a view more things to do before this works properly.

Local schema stitching will allow you to focus on your client API without having to wonder which client you have to use for which service. Also, it will allow you to form one local schema from which you can generate the types exactly like you want them.

Furthermore, there are execution features that we are currently adding like auto-batching and manual-batching. Support for subscription, ´@defer´ and persisted queries are also coming.

Last but not least we have a lot to do on the tooling side. We want to have a nice integration with all Visual Studios and are working on things like live generation. You can get a feeling for this by doing `dotnet watch build`. We have updated the watch information to exclude the generated files and include the _GraphQL_ files.

Please check it out and give us feedback so we can adjust and refine the experience further.

If you want to get into contact with us head over to our slack channel and join our community.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
