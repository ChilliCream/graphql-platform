---
title: Schema Stitching
---

**What is schema stitching actually?**

Schema stitching is the capability to merge multiple GraphQL schemas into one schema that can be queried.

# Introduction

**So, for what is that useful?**

In our case we have lots of specialized services that serve data for specific problem domains. Some of these services are GraphQL services, some of them are REST services and yes sadly a little portion of those are still SOAP services.

With Hot Chocolate schema stitching we are able to create a gateway that bundles all those services into one GraphQL schema.

**Is schema stitching basically just putting two schemas together?**

Just putting two schemas into one and avoid name collisions is simple. But what we want to achieve with schema stitching is one consistent schema.

Hot Chocolate schema stitching allows us to really integrate services into one schema by folding types into one another and even renaming or removing parts.

With this we can create a consistent GraphQL schema that hides the implementation details of our backend services and provides the consumer of our endpoint with the capability to fetch the data they need with one call, no under- or over-fetching and most importantly no repeated fetching because we first needed to fetch that special id with which we now can fetch this other thingy.

# Getting Started

In order to showcase how schema stitching works and what the problems are let us assume we have a service like twitter, where a user can post messages.

Moreover, let us assume we have three teams working on internal micro-/domain-services that handle certain aspects of that service.

The first service is handling the message stream and has the following schema:

```sdl
type Query {
  messages(userId: ID!): [Message!]
  message(messageId: ID!): Message
}

type Mutation {
  newMessage(input: NewMessageInput!): NewMessagePayload!
}

type Message {
  id: ID!
  text: String!
  createdBy: ID!
  createdAt: DateTime!
  tags: [String!]
}

type NewMessageInput {
  text: String!
  tags: [String!]
}

type NewMessagePayload {
  message: Message
}
```

The second service is handling the users of the services and has the following schema:

```sdl
type Query {
  user(userId: ID!): User!
  users: [User!]
}

type Mutation {
  newUser(input: NewUserInput!): NewUserPayload!
  resetPassword(input: ResetPasswordInput!): ResetPasswordPayload!
}

type NewUserInput {
  username: String!
  password: String!
}

type ResetPasswordInput {
  username: String!
  password: String!
}

type NewUserPayload {
  user: User
}

type ResetPasswordPayload {
  user: User
}

type User {
  id: ID!
  username: String!
}
```

Last but not least we have a third service handling the message analytics. In our example case we keep it simple and our analytics service just tracks three different counters per message. The schema for this service looks like the following:

```sdl
type Query {
  analytics(messageId: ID!, type: CounterType!): MessageAnalytics
}

type MessageAnalytics {
  id: ID!
  messageId: ID!
  count: Int!
  type: CounterType!
}

enum CounterType {
  VIEWS
  LIKES
  REPLIES
}
```

With those three separate schemas our UI team would have to fetch from multiple endpoints.

Even worse for our UI team, in order to build a stream view that shows the message text and the name of the user who posted the message, they would have to first fetch all the messages and could only then fetch the names of the users.

This is actually one of the very things GraphQL tries to solve.

# Setting up our server

Before we start with stitching itself let`s get into how to setup our server.

Every Hot Chocolate server can be a stitching server. This means in order to get started we can just use the Hot Chocolate GraphQL server template and modify it a little bit to make the server a stitching server.

If you do not have the Hot Chocolate GraphQL server template installed execute first the following command.

```bash
dotnet new -i HotChocolate.Templates.Server
```

After that we will create a new folder and add a new server to that folder.

```bash
mkdir stitching-demo
cd stitching-demo
dotnet new graphql
```

With this we have now a functioning GraphQL server with a simple hello world example.

In order to make this server a stitching server we now have to add the Hot Chocolate stitching engine.

```bash
dotnet add package HotChocolate.Stitching
```

and Subscription package if using AspNetCore

```bash
dotnet add package HotChocolate.AspNetCore.Subscriptions
```

Now that our GraphQL server is ready we can start to configure the endpoints of our remote schemas.

> Remote schemas are what we call the GraphQL schemas that we want to include into our merged schema. Remote schemas can be any GraphQL Spec compliant server (Apollo, Sangria, Hot Chocolate etc.) that serves its schema over HTTP. Also we can include local schemas that are created with the Hot Chocolate .NET API.

The endpoints are declared by using a named `HttpClient` via the HttpClient factory that is included with ASP.NET core.

```csharp
services.AddHttpClient("messages", (sp, client) =>
{
  client.BaseAddress = new Uri("http://127.0.0.1:5050");
});
services.AddHttpClient("users", (sp, client) =>
{
  client.BaseAddress = new Uri("http://127.0.0.1:5051");
});
services.AddHttpClient("analytics", (sp, client) =>
{
  client.BaseAddress = new Uri("http://127.0.0.1:5052");
});
```

Now let\`s remove the parts from the server template that we don't need and add subscriptions support.

> We will show some strategies of how to handle authenticated services later on.

```csharp
services.AddDataLoaderRegistry();

services.AddGraphQL(sp => SchemaBuilder.New().AddType<Query>().Create());

services.AddGraphQLSubscriptions();
```

# Stitching Builder

The stitching builder is the main API to configure a stitched GraphQL schema (GraphQL gateway). In order to have a simple auto-merge we have just to provide all the necessary schema names and the stitching layer will fetch the remote schemas via introspection on the first call to the stitched schema.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics"));
```

Since a stitched schema is essentially no different to any other GraphQL schema, we can configure custom types, add custom middleware or do any other thing that we could do with a Hot Chocolate GraphQL schema.

In our example we are stitching together schemas that come with non-spec scalar types like `DateTime`. So, the stitching layer would report a schema error when stitching the above three schemas together since the `DateTime` scalar is unknown.

In order to declare this custom scalar we can register the extended scalar set like with a regular Hot Chocolate GraphQL schema through the `AddSchemaConfiguration`-method on the stitching builder.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics")
  .AddSchemaConfiguration(c =>
  {
    c.RegisterExtendedScalarTypes();
  }));
```

> More information about our scalars can be found [here](/docs/hotchocolate/v10/schema/custom-scalar-types).

With this in place our stitched schema now looks like the following:

```sdl
type Query {
  messages(userId: ID!): [Message!]
  message(messageId: ID!): Message
  user(userId: ID!): User!
  users: [User!]
  analytics(messageId: ID!, type: CounterType!): MessageAnalytics
}

type Mutation {
  newMessage(input: NewMessageInput!): NewMessagePayload!
  newUser(input: NewUserInput!): NewUserPayload!
  resetPassword(input: ResetPasswordInput!): ResetPasswordPayload!
}

type Message {
  id: ID!
  text: String!
  createdBy: ID!
  createdAt: DateTime!
  tags: [String!]
}

type NewMessageInput {
  text: String!
  tags: [String!]
}

type NewMessagePayload {
  message: Message
}

type NewUserInput {
  username: String!
  password: String!
}

type ResetPasswordInput {
  username: String!
  password: String!
}

type NewUserPayload {
  user: User
}

type ResetPasswordPayload {
  user: User
}

type User {
  id: ID!
  username: String!
}

type MessageAnalytics {
  id: ID!
  messageId: ID!
  count: Int!
  type: CounterType!
}

enum CounterType {
  VIEWS
  LIKES
  REPLIES
}
```

We have just achieved a simple schema merge without doing a lot of work. But honestly we would like to change some of the types. While the stitching result is nice, we would like to integrate the types with each other.

# Schema Extensions

So, the first thing that we would like to have is a new field on the query that is called `me`. The `me` field shall represent the currently signed in user of our service.

Further, the user type should expose the message stream of the user, this way we could fetch the messages of the signed in user like the following:

```graphql
{
  me {
    messages {
      text
      tags
    }
  }
}
```

In order to extend types in a stitched schema we can use the new GraphQL extend syntax that was introduced with the 2018 spec.

```sdl
extend type Query {
  me: User! @delegate(schema: "users", path: "user(id: $contextData:UserId)")
}

extend type User {
  messages: [Message!]
  @delegate(schema: "messages", path: "messages(userId: $fields:Id)")
}
```

With just that and no further code needed we have specified how the GraphQL stitching engine shall rewrite our schema.

Let us dissect the above GraphQL SDL in order to understand what it does.

First, let us have a look at the `Query` extension. We declared a field like we would do with the schema-first approach. After that we annotated the field with the `delegate` directive. The `delegate` directive basically works like a middleware that delegates calls to to a remote schema.

The `path`-argument on the `delegate` directive specifies how to fetch the data from the remote schema. The selection path can have multiple levels. So, if we wanted to fetch just the username we could do that like the following:

```graphql
user(id: $contextData:UserId).username
```

Moreover, we are using a special variable that can access the resolver context.

Currently this variable has four scopes:

- Arguments

  Access arguments of the annotated field: `$arguments:ArgumentName`

- Fields

  Access fields of the declaring type: `$fields:FieldName`

- ContextData

  Access properties of the request context data map: `$contextData:Key`

- ScopedContextData

  Access properties of the scoped field context data map: `$scopedContextData:Key`

The context data can be used to map custom properties into our GraphQL resolvers. In our case we will use it to map the internal user ID from the user claims into our context data map. This allows us to have some kind of abstraction between the actual HttpRequest and the data that is needed to process a GraphQL request.

> Documentation on how to add custom context data from a http request can be found [here](/docs/hotchocolate/v10/execution-engine/custom-context)

OK, let\`s sum this up, with the `delegate` directive we are able to create powerful stitching resolvers without writing one line of c# code. Furthermore, we are able to create new types that make the API richer without those types having any representation in any of the remote schemas.

In order to get our extensions integrated we need to add the extensions to our stitching builder. Like with the schema we have multiple extension methods to load the GraphQL SDL from a file or a string and so on.

In our case let\`s say we are loading it from a file called `Extensions.graphql`.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics"))
  .AddExtensionsFromFile("./graphql/Extensions.graphql")
  .AddSchemaConfiguration(c =>
  {
    c.RegisterExtendedScalarTypes();
  })
```

Now with all of this in place our schema looks like the following:

```sdl
type Query {
  me: User!
  messages(userId: ID!): [Message!]
  message(messageId: ID!): Message
  user(userId: ID!): User!
  users: [User!]
  analytics(messageId: ID!, type: CounterType!): MessageAnalytics
}

type Mutation {
  newMessage(input: NewMessageInput!): NewMessagePayload!
  newUser(input: NewUserInput!): NewUserPayload!
  resetPassword(input: ResetPasswordInput!): ResetPasswordPayload!
}

type Message {
  id: ID!
  text: String!
  createdBy: ID!
  createdAt: DateTime!
  tags: [String!]
}

type NewMessageInput {
  text: String!
  tags: [String!]
}

type NewMessagePayload {
  message: Message
}

type NewUserInput {
  username: String!
  password: String!
}

type ResetPasswordInput {
  username: String!
  password: String!
}

type NewUserPayload {
  user: User
}

type ResetPasswordPayload {
  user: User
}

type User {
  id: ID!
  username: String!
  messages: [Message!]
}

type MessageAnalytics {
  id: ID!
  messageId: ID!
  count: Int!
  type: CounterType!
}

enum CounterType {
  VIEWS
  LIKES
  REPLIES
}
```

# Schema Transformations

Though this is nice, we would like to go even further and enhance our `Message` type like the following:

```sdl
type Message {
  id: ID!
  text: String!
  createdBy: User
  createdById: ID!
  createdAt: DateTime!
  tags: [String!]
  views: Int!
  likes: Int!
  replies: Int!
}
```

Moreover, we would like to remove the `analytics` field from our query type since we have integrated the analytics data directly into our `Message` type.

Since with the root field gone we have no way of accessing `MessageAnalytics` and `CounterType`, let\`s also get rid of these types.

The stitching builder has powerful refactoring functions that even can be extended by writing custom document- and type-rewriters.

In order to remove a field or a type we can tell the stitching builder to ignore them by calling one of the ignore extension methods.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics"))
  .AddExtensionsFromFile("./graphql/Extensions.graphql")
  .IgnoreField("analytics", "Query", "analytics")
  .IgnoreType("analytics", "MessageAnalytics")
  .IgnoreType("analytics", "CounterType")
  .AddSchemaConfiguration(c =>
  {
    c.RegisterExtendedScalarTypes();
  })
```

> There are also methods for renaming types and fields where the stitching engine will take care that the schema is consitently rewritten so that all the type references will refer to the corrent new type/field name.

With that we have removed the types from our stitched schema. Now, let us move on to extend our message type.

```sdl
extend type Message {
  createdBy: User!
  @delegate(schema: "users", path: "user(id: $fields:createdById)")
  views: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  likes: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  replies: Int!
  @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
}
```

Since we introduced a new field `createdBy` that basically overwrites the field that we have already declared on our original `Message` type, we need to rename the original field `createdBy` to `createdById` so that we are still able to use it.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics"))
  .AddExtensionsFromFile("./graphql/Extensions.graphql")
  .IgnoreField("analytics", "Query", "analytics")
  .IgnoreType("analytics", "MessageAnalytics")
  .IgnoreType("analytics", "CounterType")
  .RenameField("messages", "Message", "createdBy", "createdById")
  .AddSchemaConfiguration(c =>
  {
    c.RegisterExtendedScalarTypes();
  })
```

> It is important to now that the document- and type-rewriters are executed before the schemas are merged and the extensions integrated.

Our new schema now looks like the following:

```sdl
type Query {
  me: User!
  messages(userId: ID!): [Message!]
  message(messageId: ID!): Message
  user(userId: ID!): User!
  users: [User!]
}

type Mutation {
  newMessage(input: NewMessageInput!): NewMessagePayload!
  newUser(input: NewUserInput!): NewUserPayload!
  resetPassword(input: ResetPasswordInput!): ResetPasswordPayload!
}

type Message {
  id: ID!
  text: String!
  createdBy: User
  createdById: ID!
  createdAt: DateTime!
  tags: [String!]
  views: Int!
  likes: Int!
  replies: Int!
}

type NewMessageInput {
  text: String!
  tags: [String!]
}

type NewMessagePayload {
  message: Message
}

type NewUserInput {
  username: String!
  password: String!
}

type ResetPasswordInput {
  username: String!
  password: String!
}

type NewUserPayload {
  user: User
}

type ResetPasswordPayload {
  user: User
}

type User {
  id: ID!
  username: String!
  messages: [Message!]
}
```

# Query Rewriter

As can be seen, it is quite simple to stitch multiple schemas together and enhance them with the stitching builder.

**But how can we go further and hook into the query rewriter of the stitching engine?**

Let us for instance try to get rid of the `createdById` field of the `Message` type as we actually do not want to expose this field to the consumer of the stitched schema.

Since our resolver for the newly introduced `createdBy` field is dependent on the `createdById` field in order to fetch the `User` from the remote schema, we would need to be able to request it as some kind of a hidden field whenever a `Message` object is resolved.

We could then write a little field middleware that copies us the hidden field data into our scoped context data, so that we are consequently able to use the id in our `delegate` directive by accessing the `createdById` via the scoped context data instead of referring to a field of the `Message` type.

The stitching engine allows us to hook into the the query rewrite process and add our own rewrite logic that could add fields or even large sub-queries.

The first thing we need to do here is to create a new class that inherits from `QueryDelegationRewriterBase`.

The base class exposes two virtual methods `OnRewriteField` and `OnRewriteSelectionSet`.

A selection set describes a selection of fields and fragments on a certain type.

So, in order to fetch a hidden field every time a certain type is requested we would want to overwrite `OnRewriteSelectionSet`.

```csharp
private class AddCreatedByIdQueryRewriter
    : QueryDelegationRewriterBase
{
    public override SelectionSetNode OnRewriteSelectionSet(
        NameString targetSchemaName,
        IOutputType outputType,
        IOutputField outputField,
        SelectionSetNode selectionSet)
    {
        if(outputType.NamedType() is ObjectType objectType
          && objectType.Name.Equals("Message"))
        {
            return selectionSet.AddSelection(
                new FieldNode
                (
                    null,
                    new NameNode("createdBy"),
                    new NameNode("createdById"),
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ));
        }

        return selectionSet;
    }
}
```

The syntax nodes have a lot of little rewrite helpers like `AddSelection`. These helper methods basically branch of the syntax tree and return a new version that contains the applied change.

In our case we get a new `SelectionSetNode` that now also contains a field `createdBy` with an alias `createdById`. In a real-world implementation we should use a more complex alias name like `___internal_field_createdById` in order to avoid collisions with field selections of the query.

Query delegation rewriters are registered with the dependency injection and not with our stitching builder.

```csharp
services.AddQueryDelegationRewriter<AddCreatedByIdQueryRewriter>();
```

> Query delegation rewriters are hosted as scoped services and can be injected with `IStitchingContext` and `ISchema` in order to access the remote schemas or the stitched schema for advanced type information.

With that in place, the stitching engine will always fetch the requested field for us whenever a `Message` object is requested.

So, now let us move on to write a little middleware that copies this data into our scoped resolver context data map. The data in this map will only be available to the resolvers in the subtree of the message type.

A field middleware has to be declared via the stitching builder.

```csharp
services.AddStitchedSchema(builder => builder
  .AddSchemaFromHttp("messages")
  .AddSchemaFromHttp("users")
  .AddSchemaFromHttp("analytics"))
  .AddExtensionsFromFile("./graphql/Extensions.graphql")
  .IgnoreField("analytics", "Query", "analytics")
  .IgnoreType("analytics", "MessageAnalytics")
  .IgnoreType("analytics", "CounterType")
  .IgnoreField("messages", "Message", "createdBy")
  .AddSchemaConfiguration(c =>
  {
    c.RegisterExtendedScalarTypes();

    c.Use(next => async context =>
    {
        await next.Invoke(context);

        if(context.Field.Type.NamedType() is ObjectType objectType
          && objectType.Name.Equals("Message")
          && context.Result is IDictionary<string, object> data
          && data.TryGetValue("createdById", out object value))
        {
            context.ScopedContextData =
                context.ScopedContextData.SetItem("createdById", value);
        }
    })
  })
```

> We could also declare a field middleware as class. More about what can be done with a field middleware can be found [here](/docs/hotchocolate/v10/execution-engine/middleware).

With all of this in place we can now rewrite our `Message` type extension and access the `createdById` from the scoped context data:

```sdl
extend type Message {
  createdBy: User!
  @delegate(schema: "users", path: "user(id: $scopedContextData:createdById)")
  views: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  likes: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  replies: Int!
  @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
}
```

# Extending the Schema Builder

The stitching builder can be extended on multiple levels by writing different kinds of schema syntax rewriter.

## Source Schema Rewriter

The refactoring methods that we provide like `IgnoreField` or `RenameType` and so on rewrite the source schemas before they are merged.

In order to rewrite the source schema we can opt to create a `IDocumentRewriter` that is able to rewrite the whole schema document, or a `ITypeRewriter` that only can rewrite parts of a type definition.

If we wanted to delete a type or write a rewriter that also refactors the impacted types of a change then the `IDocumentRewriter` would be the way to go.

If we wanted to rewrite just parts of a type like adding some documentation or adding new fields to a type, basically things that do not impact other types, we could opt for the `ITypeRewriter`.

In both types we could opt to use the rewriter and visitor base classes that are included in our parser package.

The type rewriter provides us also with a simple way to automatically rewrite fields and decorate them with the delegation attribute.

```csharp
var path = new SelectionPathComponent(
    field.Name,
    field.Arguments.Select(t => new ArgumentNode(
        t.Name,
        new ScopedVariableNode(
            null,
            new NameNode(ScopeNames.Arguments),
            t.Name))).ToList());

field.AddDelegationPath("schemaName", path);
```

> Information about our parser can be found [here](/docs/hotchocolate/v10/advanced).

## Merged Schema Rewriter

Apart from the source schema rewriters we can also rewrite the schema document after it has been merged:

```csharp
IStitchingBuilder AddMergedDocumentRewriter(Func<DocumentNode, DocumentNode> rewrite);
```

This can be very useful if we want to first let all source schema rewriters do their work and annotate the types. With the annotations in place we could write complex rewriters that further enhance our stitched schema.

Also, if we just wanted to validate the schema for merge errors or collect information on the rewritten schema we are able to add schema visitors that run after all schema modifications are done.

```csharp
IStitchingBuilder AddMergedDocumentVisitor(Action<DocumentNode> visit);
```

## Merge Rules

In most cases the default merge rules should be enough. But with more domain knowledge about the source schemas one could write more aggressive merge rules.

The merge rules are chained and pass along what they cannot handle. The types of the various schemas are bucketed by name and passed to the merge rule chain.

# Error Handling

Errors from remote schemas are automatically rewritten and exposed as an error of the stitched schema. In order to rewrite an error correctly we need the path collection to be set; otherwise, the error will be exposed as global error.

Like with just any Hot Chocolate schema you can add error filters in order to provide more context data or to provide a better rewrite logic. Our initial rewrite logic will add the unmodified original error as property `remote` to the extensions.

## Add an error filter

```csharp
serviceCollection.AddStitchedSchema(builder =>
    builder.AddSchemaFromHttp("messages")
        .AddSchemaFromHttp("users")
        .AddSchemaFromHttp("analytics"))
        .AddExecutionConfiguration(b =>
        {
            b.AddErrorFilter(error =>
            {
                return error.AddExtension("STITCH", "SOMETHING");
            });
        }));
```

## Get the original error

```csharp
serviceCollection.AddStitchedSchema(builder =>
    builder.AddSchemaFromHttp("messages")
        .AddSchemaFromHttp("users")
        .AddSchemaFromHttp("analytics"))
        .AddExecutionConfiguration(b =>
        {
            b.AddErrorFilter(error =>
            {
                if(error.Extensions.TryGetValue("remote", out object o)
                  && o is IError originalError)
                {
                    return error.AddExtension(
                      "remote_code",
                      originalError.Code);
                }
                return error;
            });
        }));
```

> More about error filter can be found [here](/docs/hotchocolate/v10/execution-engine/error-filter).

# Authentication

In many cases schemas will be protected by some sort of authentication. In most cases http requests are authenticated with bearer tokens that are passed along as `Authorization` header.

Moreover, the most common case that we have seen so far is that people want to pass the tokens along to the remote schema.

The stitching engine creates a lazy query executor that will only start merging the schemas on the first call to the GraphQL gateway. This allows us to use the token of an incoming call to execute the introspection queries on the remote schemas. This also safes us from having to store some kind of service token with the GraphQL gateway.

In order to pass on the incoming `Authorization` header to our registered HttpClients we need to first register the HttpContext accessor from ASP.NET core.

```csharp
services.AddHttpContextAccessor();
```

Next, we need to update our HttpClient factory declaration:

```csharp
services.AddHttpClient("messages", (sp, client) =>
{
    HttpContext context = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(
                context.Request.Headers["Authorization"]
                    .ToString());
    }

    client.BaseAddress = new Uri("http://127.0.0.1:5050");
});
```

Another variant can also be to store service tokens for the remote schemas with our GraphQL gateway.

How you want to implement authentication strongly depends on your needs. With the reliance on the HttpClient factory from the ASP.NET core foundation we are very flexible and can handle multiple scenarios.

# Making HTTP clients resilient

When using stitching in production environments it is important to configure the HTTP clients to be resilient against connection losses and other HTTP errors. Since we are using Microsoft HttpClient factory, we can use `Polly` to configure retry policies and more. This is especially important if you are using external services like the GitHub GraphQL schema.

```csharp
services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
})
.AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
{
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(10)
}));
```

Microsoft provides a great documentation for Polly and we recommend to check it out: [Use HttpClientFactory to implement resilient HTTP requests](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests).

# Batching

The stitching layer transparently batches queries to the remote schemas. So, if you extend types like the following:

```sdl
extend type Message {
  views: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  likes: Int! @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
  replies: Int!
  @delegate(schema: "analytics", path: "analytics(id: $fields:id)")
}
```

We do send only a single request to your remote schema instead of three. The batching mechanism works not only within one type but extends to all requests that are executed in a resolver batch.

Furthermore, we are also including calls that are done through direct calls on the `IStitchingContext`.

Batching works very similar to _DataLoader_ where the stitching engine sends requests through the `IRemoteQueryClient` which consequently only fetches the data once the query engine signals that all resolvers have been enqueued and have registered their calls against the remote schemas. This reduces the calls to the remote-schemas significantly and improves the overall performance.

So, if we had two query calls:

Query 1:

```graphql
{
  customer(id: "abc") {
    name
    contracts {
      id
    }
  }
}
```

Query 2:

```graphql
{
  customer(id: "def") {
    name
    contracts {
      id
    }
  }
}
```

We would merge those two queries into one:

```graphql
{
  __1: customer(id: "abc") {
    name
    contracts {
      id
    }
  }
  __2: customer(id: "def") {
    name
    contracts {
      id
    }
  }
}
```

This lets the remote schema optimize the calls much better since now the remote schema could take advantage of things like _DataLoader_ etc.

# Root Types

We are currently supporting stitching `Query` and `Mutation`.

With Version 9 we will introduce stitching the `Subscription` type.

Stitching queries is straight forward and works like described earlier. Mutations are also quite straight forward, but it is often overlooked that mutations are executed with a different execution strategy.

Query resolvers are executed in parallel when possible. All fields of a query have to be side-effect free.

[GraphQL June 2018 Specification](https://facebook.github.io/graphql/June2018/#sec-Normal-and-Serial-Execution)

> Normally the executor can execute the entries in a grouped field set in whatever order it chooses (normally in parallel). Because the resolution of fields other than top‐level mutation fields must always be side effect‐free and idempotent, the execution order must not affect the result, and hence the server has the freedom to execute the field entries in whatever order it deems optimal.

The top‐level mutation fields are executed serially which guarantees that the top-level fields are executed one after the other.

```graphql
mutation {
  createUser(userName: "foo") {
    someFields
  }
  addUserToGroup(userName: "foo", groupName: "bar") {
    someFields
  }
}
```

The above example first creates a user and then adds the created user to a group. This means that mutations can only be stitched on the top level. Everything, that you stitch in the lower levels is delegating the request to a `Query` type.

Or, even simpler put, only fields that are declared on the mutation type can delegate to a mutation field on a remote query.

Let's put that in a context.

```sdl
type Mutation {
  newUser(input: NewUserInput!): NewUserPayload! @delegate(schema: "users")
}

type NewUserInput {
  username: String!
  password: String!
}

type NewUserPayload {
  user: User
}

type User {
  id: ID!
  username: String!
  messages: [Message!]
  @delegate(schema: "messages", path: "messages(userId: $fields:Id)")
}
```

In the above example we have a mutation that delegates the `newUser` field to the `newUser` mutation of the `users` schema. The mutation returns the `NewUserPayload` which has a field `user` that returns the newly created user. The `User` object delegates the `messages` field to the message schema. Since this field is resolved in the third level it will delegated to the query type of the `messages` schema.

This also means that we cannot group mutations like we could group queries. So, something like the following would not work since it is not spec-compliant:

```sdl
type Mutation {
  userMutations: UserMutations
}

type UserMutations {
  newUser(input: NewUserInput): NewUserPayload
}
```

# Stitching Context

The stitching engine provides a lot of extension points, but if we wanted to write the stitching for one specific resolver by ourselves then we could do that by using the `IStitchingContext` which is a scoped service and can be resolved through the resolver context.

```csharp
IStitchingContext stitchingContext = context.Service<IStichingContext>();
IRemoteQueryClient remoteQueryClient = stitchingContext.GetRemoteQueryClient("messages");
IExecutionResult result = remoteQueryClient.ExecuteAsync("{ foo { bar } }")
```
