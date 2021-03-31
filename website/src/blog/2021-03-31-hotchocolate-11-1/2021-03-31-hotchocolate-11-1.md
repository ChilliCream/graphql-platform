---
path: "/blog/2021/03/31/hot-chocolate-11-1"
date: "2021-03-31"
title: "ChilliCream Platform Update 11.1"
tags:
  [
    "hotchocolate",
    "strawberryshake",
    "graphql",
    "dotnet",
    "aspnetcore",
    "blazor",
  ]
featuredImage: "hot-chocolate-11-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we are releasing Hot Chocolate server and Strawberry Shake client 11.1. This release brings many things that we skipped for the initial release of Hot Chocolate server 11. The platform now contains four major components: Hot Chocolate server, Hot Chocolate gateway, Banana Cake Pop, and Strawberry Shake.

# Strawberry Shake

Let us start with the biggest new feature we built for 11.1, which is Strawberry Shake. 

What the heck is Strawberry Shake, you ask?

Well, that has changed over the time of our development on it. When we started looking at GraphQL clients, in general, and how we can bring something to .NET, we began to try out many things and experimented with the experience.

The first internal StrawberryShake was GraphQL client built on top of IQueryable. The experience felt awful since we had to create syntax to describe a GraphQL query, and it never felt natural. Ever since this first try, we were convinced to bring a better experience where GraphQL is front and center. We came away with the thought that it is best to do GraphQL with GraphQL. When we write a GraphQL query, we already have this beautiful and simple syntax that is strongly typed. The only thing we were missing is something that makes it a first citizen in the .NET IDEs.

The first public preview of Strawberry Shake began to go down this path by compiling the GraphQL queries into C# code. Still, it essentially was a glorified HttpClient.

After our first tries with Strawberry Shake, we polled our community and looked at what people want to do with a GraphQL client in .NET. There are actually three different use-cases people want to tackle with Strawberry Shake.

Build an application with GraphQL (Xamarin/Blazor)
Do server to server communication
Write unit tests against a GraphQL server

When we polled our users, we found that 1 and 2 have an almost equal share of people. Use-Case 2 is a bit bigger. The group that wants to write tests with it is the smallest.

When we restarted the development on Strawberry Shake, we thought building a GraphQL client for the first group would allow us to disrupt the ecosystem the most. Something like Relay or Apollo client is completely missing in the .NET ecosystem. If we look at patterns and how UIs are built in .NET, we see that it is over complicated to achieve these reactive UIs that work even when your application goes offline with optimistic mutations.

So, for version 11.1, we set the focus on .NET frontend developers.

When you ask me now what Strawberry Shake is, I would say it is a state management component.

## State and Entities

Strawberry Shake understands your schema and knows what your entities are. When you interact with your data through Strawberry Shake, you are really interacting against a store that holds this data. The data in this store can be local data or remote data.

GRAPHIC

When you write a GraphQL query, we will compile it into C# code. The generated client will know how to decompose the response of your queries into entities. Strawberry Shake knows which query holds the data of which entity.

GRAPHIC

## How it works

Let us have a look at how this all works and make some sense of this long introduction.

When we write a query like the following:

EXAMPLE GRAPHQL

We compile the GraphQL operation to a .NET client where each operation becomes a class that can be executed.

EXAMPLE CODE EXECUTE

This essentially is what we could do with the first public GraphQL client iteration.

But I talked about state and how we understand data. Meaning we can also subscribe to our data.

EXAMPLE CODE WATCH

In this case, we are subscribing to our store and triggering an update to this store by fetching new data from the GraphQL server.

GRAPHIC

Whenever entities are changing that make up our operation response, the store will trigger our subscribe delegate, which in consequence will update our UI component.
 
Entities are changing whenever ANY request is made to the backend, whether it is a real-time request through subscriptions or just a mutation that is changing the data we are watching. For our application development, this means that we do NOT need to make unnecessary re-fetches or build complicated logic to update all the components where some data is displayed. We are just subscribing to the data, and whenever it changes, all components that display that particular piece of information are updated.

## Execution Strategies

Apart from our data's reactivity, we can also use the store to control when data is fetched.  By default, Strawberry Shake will always first fetch from the network before it accepts updates to entities it is watching.  It would often be more efficient if we first looked at our store and used the data that is already in our memory and at the same time started updating this data. This would lead to a more responsive UI component that has, in most cases, something to display right out of the gate.

We call this strategy `CacheAndNetwork`.

EXAMPLE

Last but not least we have a third strategy to access data which is called `CacheFirst`. This strategy will look at the store first and use the data we already have. Only if the store has no data for the request we are executing will we go to the network to fetch new data.

## Persistence

The last aspect that I want to go into is store persistence. The store that we built into Strawberry Shake can also be persisted. We provide out-of-the-box a package to use SQLite to persist your data. Persisting your store can create true offline applications that fetch new data while online and preserve this data while offline. It also allows you to have faster startup times with your online applications since you can combine this with the `CacheAndNetwork` strategy, so whenever your mobile app starts, the user will immediately have data that will be updated in the background without you having to write all this complicated code.

Adding this capability to your application is now really two lines of code:

EXAMPLE

## Outlook

This is the first real version of Strawberry Shake, and we have planned a lot more for it. 

With 11.2, we are aiming at smoothening any rough edges around the tooling. Moreover, we bring a generator option to generate the client without the store for server-to-server use-cases.

For the next major release, we are looking to bring @stream, @defer, and the MultiPart request specification to StrawberryShake. All things we already support with the Hot Chocolate server. Further, we want to bring more protocols like subscriptions over SignalR and gRPC.

If you want to get started with strawberry shake or read more about its capabilities head over to out documentation. 

Strawberry Shake was mostly built by Pascal, Fred, Rafael, and me.

# Hot Chocolate

While we focused on Strawberry Shake for this release, we also invested further into our GraphQL server, Hot Chocolate.

# .NET Support

With version 11.1, we started compiling with the .NET 6 SDK, meaning that we target in our ASP.NET core components, .NET 6, .NET 5, and .NET Core 3.1. The GraphQL core and the parsers are still also compiled for .NET Standard 2.0. Further, all our client utilities are compiled for .NET Standard 2.0 as well to let you consume GraphQL in almost any .NET application.

# Performance

As with every release, we are putting a lot of energy into performance. With performance, we mean both execution time and memory usage. For this release, we looked at static memory usage. Essentially the memory footprint of Hot Chocolate when you just create the schema. When we started to work on this, Hot Chocolate used around 380.000 objects to create the GitHub schema. Now with version 11.1, we are only using around 80.000 objects to represent the same schema. We also reduced the schema memory usage by around 40%. We identified a lot more improvements that we can do in this area but where we would need to more substantially change how we build a schema. Beginning with version 12, we will use source generators in a lot of these areas in the server to achieve faster execution and a lower memory footprint.

# GraphQL MultiPart request specification

With version 11.1, we now support out-of-the-box the [GraphQL MultiPart request specification], which allows handling file streams in GraphQL requests.

When using the `HotChocolate.AspNetCore` package, your server out-of-the-box supports this new specification, no need to opt-in. In order to use the new capabilities, you need to register the `Upload` scalar.

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddQueryType<UploadQuery>(); // <--- this registers the new scalar
```

To separate the `Upload` scalar from the ASP.NET core dependencies on all things multi-part, we have put the actual scalar into the [HotChocolate.Types.Scalars.Upload] package, which can be used in .NET Standard 2.0 and has only a dependency on [HotChocolate.Types]. 

When using the new type in our annotation-based approach, you only need to use the new interface `IFile`.

```csharp
public record CreateNewUserInput(string Username, IFile ProfilePicture);

public class Mutation
{
    public async Task<NewUserPayload> CreateNewUser(CreateNewUserInput input)
    {
        using Stream stream = input.OpenReadStream();
        // do your work with the streamed file here
    }
}
```

You can also use `IFile` directly as an argument or in lists.

For code-first, when you want to declare this explicitly, you can use the actual type `UploadQuery`,

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("bar")
            .Argument("file", a => a.Type<UploadType>())...
    }
}
```

Finally, in schema-first, you can use the name of the scalar `Upload`.

EXAMPLE

Most of the work on this feature was done by Tobias Tengler, who is one of our community members. He worked like most of us in his free time on this. Thank you, Tobias; we will put your code to good use.

# Scalars, Scalars, Scalars

We looked at the wider community and what problems many of you are facing. We often need to build for our specific use-cases new scalars that represent a specific domain need. We found by chance an excellent package of scalars build by [The Guild] for the JavaScript ecosystem. With version 11, we have started porting their scalars one by one over to Hot Chocolate. But fear not, we are not polluting the GraphQL core libraries with these new scalars. If you do not have any need for them, we will not bother you with this amazing set of scalars.

The new collection of scalars are published in the package [HotChocolate.Types.Scalars].

**New Scalars:**

| Type             | Description                                                                                                                                                                                                             |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EmailAddress     | The `EmailAddress` scalar type represents an email address, represented as UTF-8 character sequences that follows the specification defined in RFC 5322.                                                                 |
| HexColor         | The `HexColor` scalar type represents a valid HEX color code.                                                                                                                                                           |
| Hsl              | The `Hsl` scalar type represents a valid a CSS HSL color as defined here https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#hsl_colors.                                                                       |
| Hsla             | The `Hsla` scalar type represents a valid a CSS HSLA color as defined here https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#hsl_colors.                                                                     |
| IPv4             | The `IPv4` scalar type represents a valid a IPv4 address as defined here https://en.wikipedia.org/wiki/IPv4.                                                                                                            |
| IPv6             | The `IPv6` scalar type represents a valid a IPv6 address as defined here [RFC8064](https://tools.ietf.org/html/rfc8064).                                                                                                |
| Isbn             | The `ISBN` scalar type is a ISBN-10 or ISBN-13 number: https:\/\/en.wikipedia.org\/wiki\/International_Standard_Book_Number.                                                                                            |
| LocalDate        | The `LocalDate` scalar type represents a ISO date string, represented as UTF-8 character sequences yyyy-mm-dd. The scalar follows the specification defined in RFC3339.                                                 |
| LocalTime        | The `LocalTime` scalar type is a local time string (i.e., with no associated timezone) in 24-hr `HH:mm:ss]`.                                                                                                            |
| MacAddress       | The `MacAddess` scalar type represents a IEEE 802 48-bit Mac address, represented as UTF-8 character sequences. The scalar follows the specification defined in [RFC7042](https://tools.ietf.org/html/rfc7042#page-19). |
| NegativeFloat    | The `NegativeFloat` scalar type represents a double‐precision fractional value less than 0.                                                                                                                             |
| NegativeInt      | The `NegativeIntType` scalar type represents a signed 32-bit numeric non-fractional with a maximum of -1.                                                                                                               |
| NonEmptyString   | The `NonNullString` scalar type represents non-empty textual data, represented as UTF‐8 character sequences with at least one character.                                                                                |
| NonNegativeFloat | The `NonNegativeFloat` scalar type represents a double‐precision fractional value greater than or equal to 0.                                                                                                           |
| NonNegativeInt   | The `NonNegativeIntType` scalar type represents a unsigned 32-bit numeric non-fractional value greater than or equal to 0.                                                                                              |
| NonPositiveFloat | The `NonPositiveFloat` scalar type represents a double‐precision fractional value less than or equal to 0.                                                                                                              |
| NonPositiveInt   | The `NonPositiveInt` scalar type represents a signed 32-bit numeric non-fractional value less than or equal to 0.                                                                                                       |
| PhoneNumber      | The `PhoneNumber` scalar type represents a value that conforms to the standard E.164 format as specified in: https://en.wikipedia.org/wiki/E.164.                                                                       |
| PositiveInt      | The `PositiveInt` scalar type represents a signed 32‐bit numeric non‐fractional value of at least the value 1.                                                                                                          |
| PostalCode       | The `PostalCode` scalar type represents a valid postal code.                                                                                                                                                            |
| Port             | The `Port` scalar type represents a field whose value is a valid TCP port within the range of 0 to 65535.                                                                                                               |
| Rgb              | The `RGB` scalar type represents a valid CSS RGB color as defined here [MDN](<https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()>).                                                          |
| Rgba             | The `RGBA` scalar type represents a valid CSS RGBA color as defined here [MDN](<https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()>).                                                        |
| UnsignedInt      | The `UnsignedInt` scalar type represents an unsigned 32‐bit numeric non‐fractional value greater than or equal to 0.                                                                                                     |
| UnsignedLong     | The `UnsignedLong` scalar type represents an unsigned 64‐bit numeric non‐fractional value greater than or equal to 0.                                                                                                    |
| UtcOffset        | The `UtcOffset` scalar type represents a value of format `±hh:mm`.                                                                                                                                                      |

Most of the work on this new library was done by [Gergory], who also put his free time into Hot Chocolate. We are happy to have you onboard, Gregory!

> More about this topic can be read [here](../../docs/hotchocolate/defining-a-schema/scalars.md).

# Type Extensions

For a long time, we have type extensions that essentially let you split up types into separate type definitions. Until now, they were bound by name and could just provide new fields to existing types. This is quite useful if you want to modularize your schema and have types from different modules extend each other.

When using the annotation-based approach, we so far could do something like the following:

```csharp
public class Session
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Abstract { get; set; }

    public int? TrackId { get; set; }
}
```

Let's say `Session` is a domain entity. We do not want any GraphQL on it. But we do want to extend upon it. I know we could use code-first with our fluent API or schema-first and get this setup. But we also are able to create another type like the following:

```csharp
[ExtendObjectType(nameof(Session))]
public class SessionResolvers
{
    public async Task<Track> GetTrackAsync(
        [Parent] Session session,
        TrackByIdDataLoader trackById,
        CancellationToken cancellationToken) =>
        trackById.LoadAsync(session.TrackId, cancellationToken)
}
```

This essentially would then be merged by the schema builder into the following GraphQL type:

```sdl
type Session {
  id: Int!
  title: String!
  abstract: String
  trackId: Int
  track: Track
}
```

While this is nice, we actually do not want `trackId` and would like to replace `trackId` with `track`.

With version 11.1, we can now do that by binding the resolver to the field of the original type.

```csharp
[ExtendObjectType(nameof(Session))]
public class SessionResolvers
{
    [BindProperty(nameof(Session.TrackId))]
    public async Task<Track> GetTrackAsync(
        [Parent] Session session,
        TrackByIdDataLoader trackById,
        CancellationToken cancellationToken) =>
        trackById.LoadAsync(session.TrackId, cancellationToken)
}
```

This now leads to our new GraphQL type:

```sdl
type Session {
  id: Int!
  title: String!
  abstract: String
  track: Track
}
```

We can also now globally ignore members from the original type without binding them to a new resolver on our extension type.

```csharp
[ExtendObjectType(
    nameof(Session),
    IgnoreProperties = new[] { nameof(Session.Abstract) })]
public class SessionResolvers
{
    [BindProperty(nameof(Session.TrackId))]
    public async Task<Track> GetTrackAsync(
        [Parent] Session session,
        TrackByIdDataLoader trackById,
        CancellationToken cancellationToken) =>
        trackById.LoadAsync(session.TrackId, cancellationToken)
}
```

This leads to the following GraphQL type:

```sdl
type Session {
  id: Int!
  title: String!
  track: Track
}
```

We added one more thing to the new type extension API, and this also works in code-first with the fluent API.

We now can rewrite with type extensions multiple types at once by using base types or interfaces by doing the following:

```csharp
public class Session : IHasResourceKey
{
    public int Id { get; set; }

    public string? Key { get; set; }
}

public class Speaker : IHasResourceKey
{
    public int Id { get; set; }

    public string? Key { get; set; }
}

[ExtendObjectType(typeof(IHasResourceKey))]
public class HasResourceKeyResolvers
{
    [BindProperty(nameof(Session.Key))]
    public async Task<string?> GetDescriptionAsync(...)
        // ... omitted for brevity
}
```

The GraphQL SDL representation would now look like the following:

```sdl
type Session {
  id: Int!
  description: String
}

type Speaker {
  id: Int!
  description: String
}
```

We can also use the new type extension API to extend all the entities in our schema with the node interface and add a custom node resolver.

```csharp
[Node]
[ExtendObjectType(typeof(IEntity))]
public class EntityExtension2
{
    // this is how the node field shall resolve this entity from the
    // database ...
    [NodeResolver]
    public static IEntity GetEntity(int id) => ...
}
```

We can also have a specific entity resolver for each specific entity:

```csharp
[Node]
[ExtendObjectType(typeof(IEntity))]
public class EntityExtension2
{
    [NodeResolver]
    public static Session GetSession(int id) => ...

    [NodeResolver]
    public static Speaker GetSpeaker(int id) => ...
}
```

As I initially said, a lot of these thing could already be achieved by using the fluent API or the more complex `TypeInterceptor`. With the new capabilities of the type extension API, we can now rewrite files very simply and with less boilerplate.

It also completes the annotation-based approach further and gives us more tools to create schemas with only C#.

> More about this topic can be read [here](../../docs/hotchocolate/defining-a-schema/extending-types.md).

# MongoDB integration

As with almost every release, we are further investing in our data integration layer. Version 11.1 is now embracing MongoDB even further with native query support. Until now, you could use MongoDB with filtering, sorting, and projections through their queryable provider. But the queryable provider has many shortcomings and does not support all the features of MongoDB. With the new integration, we are rewriting the GraphQL queries into native MongoDB queries. Meaning we are building up a BSON object representing the query.

A GraphQL query like the following,

```graphql
query GetPersons {
  persons(
    where: {
      name: { eq: "Yorker Shorton" }
      addresses: { some: { street: { eq: "04 Leroy Trail" } } }
    }
  ) {
    name
    addresses {
      street
      city
    }
  }
}
```

is rewritten into the Mongo query:

```json
{
  "find": "person",
  "filter": {
    "Name": { "$eq": "Yorker Shorton" },
    "Addresses": { "$elemMatch": { "Street": { "$eq": "04 Leroy Trail" } } }
  }
}
```

To use the new Mongo integration, you need to add the [HotChocolate.Data.MongoDb] package to your project.

You can build up queries with the native driver and then create an executable from them, representing a rewritable query to Hot Chocolate.

```csharp
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons([Service] IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}

[UseFirstOrDefault]
public IExecutable<Person> GetPersonById(
    [Service] IMongoCollection<Person> collection,
    Guid id)
{
    return collection.Find(x => x.Id == id).AsExecutable();
}
```

This feature was implemented by Pascal, who is the third person who became a Chilli. Together Pascal and I are building most of the Hot Chocolate server and gateway.

> More about this topic can be read [here](../../docs/hotchocolate/defining-a-schema/extending-types.md).

# Directive Introspection

We are always looking at new GraphQL features very early. But this time, we got in even earlier and picked up an experimental feature that could change entirely or might be dropped. We are following in this GraphQL-Java.

Essentially this represents an experiment to allow users to query directives through introspection.

In order to enable this feature, you need to opt into it by enabling this in the options.

In order to activate it do the following:

```csharp
services
    .AddGraphQL()
    .AddDocumentFromString(
        @"
            type Query {
                foo: String
                    @foo
                    @bar(baz: ""ABC"")
                    @bar(baz: null)
                    @bar(quox: { a: ""ABC"" })
                    @bar(quox: { })
                    @bar
            }

            input SomeInput {
                a: String!
            }

            directive @foo on FIELD_DEFINITION

            directive @bar(baz: String quox: SomeInput) repeatable on FIELD_DEFINITION
        ")
    .UseField(next => ...)
    .ModifyOptions(o => o.EnableDirectiveIntrospection = true);
```

This would now allow you to then query all directives on your type system like the following:

```graphql
{
  __schema {
    types {
      fields {
        appliedDirectives {
          name
          args {
            name
            value
          }
        }
      }
    }
  }
}
```

But often, we do not want to expose all of our directives. For instance, we might want to hide our internal `@authorize` directives, which refer to our security policies.

In this case, we can add another option to define the default visibility of directives.

```csharp
.ModifyOptions(o =>
{
    o.EnableDirectiveIntrospection = true;
    o.DefaultDirectiveVisibility = DirectiveVisibility.Internal;
});
```

With this setting in place, we no need to mark directives that we want to query publicly.

```csharp
private sealed class UpperDirectiveType : DirectiveType
{
    protected override void Configure(
        IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("upper");
        descriptor.Public() // <-- marks the directive as publicly visible
        descriptor.Location(DirectiveLocation.Field);
        descriptor.Use(next => async context =>
        {
            await next.Invoke(context);

            if (context.Result is string s)
            {
                context.Result = s.ToUpperInvariant();
            }
        });
    }
}
```

You can even hide directives on runtime based on the user's permission. But as said before, all of this is experimental, and we will see how far this feature goes or how it will change over time.

# Summing up

Version 11.1 again is a significant update to the platform and has many more things packed that I did not have the time to list here. 

Version 11.2 will mainly round out features of 11.1. The next major update is planned for the end of June 2021 and will focus on distributed schemas, Neo4J, and Banana Cake Pop. With the June update, we will finally bring a release version of Banana Cake Pop that will pack many new things.

We are doing as before a community gathering where we will walk you through all things new to version 11.1. You can join us by signing up for our [hot chocolate 11.1 launch party]. 

[hot chocolate 11 launch party]: https://www.meetup.com/ChilliCream-User-Group/events/274656703/
[graphql multipart request specification]: https://github.com/jaydenseric/graphql-multipart-request-spec
[hotchocolate.types.scalars.upload]: https://www.nuget.org/packages/HotChocolate.Types.Scalars.Upload/
[hotchocolate.types]: https://www.nuget.org/packages/HotChocolate.Types/
[hotchocolate.types.scalars]: https://www.nuget.org/packages/HotChocolate.Types.Scalars/
[hotchocolate.data.mongodb]: https://www.nuget.org/packages/HotChocolate.Data.MongoDb/
[the guild]: https://the-guild.dev
[gregory]: https://twitter.com/wonbyte
