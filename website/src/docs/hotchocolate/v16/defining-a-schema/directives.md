---
title: "Directives"
---

Directives provide a way to add metadata for client tools such as code generators and IDEs or alternate a GraphQL server's runtime execution and type validation behavior.

There are two kinds of directives, executable directives to annotate executable parts of GraphQL documents and type-system directives to annotate SDL types.

Typically, any GraphQL server implementation should provide the following directives `@skip`, `@include`, and `@deprecated`. `@skip` and `@include`, for example, are executable directives used in GraphQL documents to exclude or include fields, whereas `@deprecated` is a type-system directive used in SDL types to inform client tools that a particular part such as a field is deprecated.

# Structure

Directives consist of a name and zero or more arguments. `@skip`, for example, has the name **skip** and a mandatory argument named **if**. Also, `@skip` carries a piece of hidden information only examinable in SDL, namely the location, which specifies where a directive is applicable. Let's take a look at the SDL of the `@skip` directive.

```sdl
directive @skip(if: Boolean!) on
    | FIELD
    | FRAGMENT_SPREAD
    | INLINE_FRAGMENT
```

The `directive` keyword in SDL indicates that we're dealing with a directive type declaration. The `@` sign also indicates that this is a directive but more from a usage perspective.

The word `skip` represents the directive's name followed by a pair of parentheses that includes a list of arguments, consisting, in our case, of one argument named `if` of type non-nullable boolean (meaning it is required).

The `on` keyword indicates the location where or at which part a directive is applicable, followed by a list of exact locations separated by pipes `|`. In the case of `@skip`, we can see that we're dealing with an executable directive because this directive is only applicable to fields, fragment-spreads, and inline-fragments.

# Usage

Let's say we have a GraphQL document and want to exclude details under certain circumstances; it would probably look something like this.

```graphql
query me($excludeDetails: Boolean!) {
  me {
    id
    name
    ...Details @skip(if: $excludeDetails)
  }
}

fragment Details on User {
  mobileNumber
  phoneNumber
}
```

With `@skip`, we've successfully altered the GraphQL's runtime execution behavior. If `$excludeDetails` is set to `true`, the execution engine will exclude the fields `mobileNumber` and `phoneNumber`; the response would look like this.

```json
{
  "data": {
    "me": {
      "id": "VXNlcgox",
      "name": "Henry"
    }
  }
}
```

Now that we know how to use directives in GraphQL, let's head over to the next section, which is about one crucial aspect of directives.

## Order Matters

**The order of directives is significant**, because the execution is in **sequential order**, which means one after the other. If we have something like the following example, we can see how directives can affect each other.

```graphql
query me {
  me {
    name @skip(if: true) @include(if: true)
  }
}
```

Since we excluded the field `name` in the first place, `@include` does not affect the field `name` anymore. We then just get an empty `me` object in return.

```json
{
  "data": {
    "me": {}
  }
}
```

> **Note:** We will have a deep dive on directives' order under the [Middleware](#order) section.

Now that we have a basic understanding of what directives are, how they work, and what we can do with them, let's create a custom directive.

# Custom Directives

To create a directive, we need to create a new class that inherits from `DirectiveType` and also to override the `Configure` method.

```csharp
public class MyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);
    }
}
```

[Learn more about Locations](#locations)

We also have to register the directive explicitly.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDirectiveType<MyDirectiveType>();
```

Let's recap! We have registered a new directive named `my` without any arguments and limited the usage to fields only. A GraphQL query request with our new directive could look like this.

```graphql
query foo {
  bar @my
}
```

As of now, our custom directive provides no functionality. We will handle that part in the [Middleware](#middleware) section. But before that, let's talk about repeatable directives and arguments.

## Repeatable

By default, directives are not repeatable, which means directives are unique and can only be applied once at a specific location. For example, if we use the `my` directive twice at the field `bar`, we will encounter a validation error. So the following GraphQL query request results in an error if the directive is not repeatable.

```graphql
query foo {
  bar @my @my
}
```

We can enable repeatability like the following.

```csharp
public class MyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);
        descriptor.Repeatable();
    }
}
```

This configuration will translate into the following SDL.

```sdl
directive @my repeatable on FIELD
```

## Arguments

A directive can provide additional information through arguments.
They might also come in handy, in combination with repeatable directives, for reusability purposes.

We can add an argument like the following.

```csharp
public class MyDirective
{
    public string Name { get; set; }
}

public class MyDirectiveType : DirectiveType<MyDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<MyDirective> descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.FieldDefinition);

        // The 'Name' property is included as an argument implicitly

        // descriptor
        //     .Argument(f => f.ChangeMe)
        //     .Type<NonNullType<StringType>>()
        //     .Name("differentName");
        // descriptor.Ignore(f => f.IgnoreMe);
    }
}
```

If we prefer to not use a backing POCO (`<T>`) we an also use the `Argument()` method on the `descriptor`.

```csharp
public class MyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);

        descriptor
            .Argument("name")
            .Type<NonNullType<StringType>>();
    }
}
```

This configuration will translate into the following SDL.

```sdl
directive @my(name: String!) on FIELD
```

## Usage within Types

We could associate the `MyDirectiveType` with an object type like the following.

```csharp
public class FooType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Directive("my", new ArgumentNode("name", "bar"));
    }
}
```

> Note: For this to work the `MyDirectiveType` directive needs to have the appropriate location within the schema. In this example it would be `DirectiveLocation.Object`.

Referencing directives using their name is not type-safe and could lead to runtime errors, which are avoidable by using our generic variant of the directive type.

Once we have defined our directive using `DirectiveType<T>`, we can pass an instance of the backing POCO (`<T>`) instead of the name of the directive and an `ArgumentNode`.

```csharp
public class FooType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Directive(new MyDirective { Name = "bar" });
    }
}
```

Since the directive instance that we have added to our type is now a strong .NET type, we don't have to fear changes to the directive structure or name anymore.

## Locations

A directive can define one or multiple locations, where it can be applied. Multiple locations are separated by a pipe `|`.

```csharp
descriptor.Location(DirectiveLocation.Field | DirectiveLocation.Object);
```

Generally we distinguish between two types of locations: Type system and executable locations.

### Type System Locations

Type system locations specify where we can place a specific directive in the schema. The arguments of directives specified in these locations are fixed. We can query such directives through introspection.

The following schema shows where type system directives can be applied.

```sdl
directive @schema on SCHEMA
directive @object on OBJECT
directive @argumentDefinition on ARGUMENT_DEFINITION
directive @fieldDefinition on FIELD_DEFINITION
directive @inputObject on INPUT_OBJECT
directive @inputFieldDefinition on INPUT_FIELD_DEFINITION
directive @interface on INTERFACE
directive @enum on ENUM
directive @enumValue on ENUM_VALUE
directive @union on UNION
directive @scalar on SCALAR
schema @schema {
  query: Query
}
type Query @object {
  search(by: SearchInput! @argumentDefinition): SearchResult @fieldDefinition
}
input SearchInput @inputObject {
  searchTerm: String @inputFieldDefinition
}
interface HasDescription @interface {
  description: String
}
type Product implements HasDescription {
  added: DateTime
  description: String
}
enum UserKind @enum {
  Administrator @enumValue
  Moderator
}
type User {
  name: String
  userKind: UserKind
}
union SearchResult @union = Product | User
scalar DateTime @scalar
```

### Executable Locations

Executable locations specify where a client can place a specific directive, when executing an operation.

Our server defines the following directives.

```sdl
directive @query on QUERY
directive @field on FIELD
directive @fragmentSpread on FRAGMENT_SPREAD
directive @inlineFragment on INLINE_FRAGMENT
directive @fragmentDefinition on FRAGMENT_DEFINITION
directive @mutation on MUTATION
directive @subscription on SUBSCRIPTION
```

The following request document shows where we, as a client, can apply these directives.

```graphql
query getUsers @query {
  search(by: { searchTerm: "Foo" }) @field {
    ...DescriptionFragment @fragmentSpread
    ... on User @inlineFragment {
      userKind
    }
  }
}

fragment DescriptionFragment on HasDescription @fragmentDefinition {
  description
}

mutation createNewUser @mutation {
  createUser(input: { name: "Ada Lovelace" }) {
    user {
      name
    }
  }
}

subscription subscribeToUser @subscription {
  onUserChanged(id: 1) {
    user {
      name
    }
  }
}
```

## Middleware

What makes directives in Hot Chocolate very useful is the ability to associate a middleware with it. A middleware can alternate the result, or even produce the result, of a field. A directive middleware is only added to a field middleware pipeline when the directive was annotated to the object definition, the field definition or the field.

Moreover, if the directive is repeatable the middleware will be added multiple times to the middleware allowing to build a real pipeline with it.

In order to add a middleware to a directive we could declare it with the descriptor as a delegate.

```csharp
public class MyDirectiveType : DirectiveType<MyDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<MyDirective> descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Object);

        descriptor.Use((next, directive) => context =>
        {
            context.Result = "Bar";
            return next.Invoke(context);
        });
    }
}
```

Directives with middleware or executable directives can be put on object types and on their field definitions or on the field selection in a query. Executable directives on an object type will replace the field resolver of every field of the annotated object type.

### Order

In GraphQL the order of directives is significant and with our middleware we use this order to create a resolver pipeline through which the result flows.

The resolver pipeline consists of a sequence of directive delegates, called one after the other.

Each delegate can perform operations before and after the next delegate. A delegate can also decide to not pass a resolver request to the next delegate, which is called short-circuiting the resolver pipeline.
Short-circuiting is often desirable because it avoids unnecessary work.

The order of the middleware pipeline is defined by the order of the directives. Since executable directives will flow from the object type to its field definitions, the directives of the type would be called first in the order that they were annotated.

```sdl
type Query {
  foo: Bar
}

type Bar @a @b {
  baz: String @c @d
}
```

So, the directives in the above example would be called in the following order `a, b, c, d`.

If there were more directives in the query, they would be appended to the directives from the type.

```graphql
{
  foo {
    baz @e @f
  }
}
```

So, now the order would be like the following: `a, b, c, d, e, f`.

Every middleware can execute the original resolver function by calling `ResolveAsync()` on the `IDirectiveContext`.

<!-- spell-checker:ignore VXNlcgox -->
