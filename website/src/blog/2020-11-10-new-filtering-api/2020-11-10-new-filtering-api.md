---
path: "/blog/2020/11/10/new-filtering-api"
date: "2020-11-10"
title: "The new Filtering API"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore", "filtering"]
featuredImage: "banner-new-filtering-api.png"
author: Pascal Senn
authorUrl: https://github.com/pascal_senn
authorImageUrl: https://avatars0.githubusercontent.com/u/14233220
---

With version 11, we release a complete rewrite of filtering, sorting, and selections. With our initial release a few versions back, we decided to use a similar syntax as Prisma did. Initially, this looked like a very intuitive way of declaring filters. We already shipped some extensions in preview releases of version 11, like object filters, list filters, etc.

We started investigating into opening up the API for users who want to provide their filters or write their database providers for Hot Chocolate. Quickly we realized that the API was not good enough for a public release and, even worse, the underlying GraphQL syntax was not ideal to use.

This was a huge setback for us, but we still went back to the drawing board and made a complete redesign of it. We looked at many different implementations of similar features, and combined with the experience we made; we settled on a similar approach to Hasura or Postgraphile.

The main issue with the filters released with version 10 is the strict binding of field and operation. The discussion and a detailed description of the problem we faced can be followed in this [Issue on GitHub](https://github.com/ChilliCream/hotchocolate/issues/2044)

Here is a quick summary:

This approach works great with scalar filters.

```graphql
where: {
    foo_contains: “bar”
}
```

We bundled the field and the operation together into an easy to understand and straight forward GraphQL field.

Object filters would add another level of nesting:

```graphql
where: {
    foo: {
        bar_contains:”bar”
    }
}
```

For array filters, we came up with a mixture of nesting and bundling. With list filters, the problems already begin to start. It is already required to have helper (`el_XXX`) syntax to access the elements of a list:

```graphql
where: {
    foo_some: {
        el_gt:4
    }
}
```

As soon as we dived deeper into possible extensions, the problems became more severe, and the API became more inconsistent. A good example of this issue is when we want to filter by the length of a string. We could filter by `foo_length_gt:4` or `foo_length: { is_gt: 4}` or even `foo: { length: { is_gt:4 } }`. All of these approaches would follow the style guide. The first would be like we define filters for the field, the second similar to the list filters, and the last one would be like the object filters.

# The New Filtering

With the new filtering API, there is a fundamental change. Operations and fields are no longer bundled together into one GraphQL field.

Here is a quick overview of the examples listed above:

Scalar filters:

```graphql
where: {
    foo: {
        contains: “bar”
    }
}
```

Object filters:

```
where: {
    foo: {
        bar {
            contains: “bar”
        }
    }
}
```

List filters:

```graphql
where: {
    foo: {
        some: {
            gt: 4
        }
    }
}
```

As the API now is based on nesting, every combination of field and operation feels a lot more natural. When you like to filter by the length of a string, the resulting API looks seamless:

```graphql
where: {
    foo: {
        length: {
            gt: 4
        }
    }
}
```

# THIS IS BREAKING MY API!

We know. We had a long discussion about this. We feel confident that this new approach is the right way to go, and it is designed to stay. The 10.X.X filters are still available in version 11. They will be deprecated, though, and will be removed in version 12.

# The Data Package

With version 11, we introduce a new package for Hot Chocolate. We created a new package called `HotChocolate.Data`. This package contains `HotChocolate.Data.Filtering`, `HotChocolate.Data.Sorting` and `HotChocolate.Data.Projections`.

# Migrating from 10 to 11

We could not avoid conflicts in type names between the old and the new filtering. You can use static imports or fully qualified type names to have the old and the new filtering API in the same file.

If you have full control over the front end, the easiest way to migrate is to replace the old filtering with the new one and make the necessary changes.

If this is not an option for you, you will have to declare new fields and deprecate the old ones once they are no longer used. You may even use the filters on the same fields, but you will end up with conflicting argument names.

# Getting started

You first need to add the new `HotChocolate.Data` package to the project.

It is also required to register filtering on the schema builder:

```csharp
public void ConfigureServcies(IServiceCollection services) {
    services.AddGraphQLServer()
        .AddQueryType<Query>()
        .AddFiltering();
}
```

You are now all set and ready to use the filters. For a pure code first approach, you can use the attribute `[UseFiltering]`, and for code first, you can use the `UseFiltering()` extension method.

```csharp
// pure code first
public class Query {
    [UseFiltering]
    public IQueryable<Foo> Foos([Service]DbContext context) => context.Foos;
}

//code first
public class Query : ObjectType {
    protected override void Configure(IObjectTypeDescriptor descriptor) {
        descriptor
            .Field<Reslover>(x => x.Foos(default!))
            .UseFiltering();
    }

    public class Resolver {
        public IQueryable<Foo> Foos([Service]DbContext context) => context.Foos;
    }
}
```

# How does it work?

The old filtering was bundling a field and operation together. With the new filtering, this is now separated. The concept of field and operation still exists, though a little different. A field is always used for navigation. You can think of it as a selector. In code first, a field represents a property of a class. An operation is always an action in the context of a field. Semantically you can look at it as a function. This is often a compare operation, like equals or greater than, but it can also be more arbitrary. In spatial data, many functions can be translated to database queries, like `ConvexHull()` or `Distance(Geometry g)`. Filtering on spatial data is something we plan to support soon. Operations are identified by an integer, which is called the operation ID.

In most cases, a filter type either only contains fields or only operations, but it is in no way restricted to that. A filter type can contain both. This can be useful to provide the necessary metadata. Let's continue the example `Distance(Geometry g)` from above. This function has a parameter `g`. To calculate the distance between to points, the consumer needs to provide one point. The function then returns the distance between these two points. In GraphQL, this now can be combined into one input type:

```graphql
input HouseFilterInputType {
    position: PointFilterInputType
}

input PointFilterInputType {
    distanceTo: DistanceToFilterInputType;
}

input DistanceToFilterInputType {
    """The other point where the distance is calculated to"""
    other: GeometryInputType!
    eq: Float
    neq: Float
    gt: Float
    ....
}
```

The new version of filtering does not only have a new look and feel at the API level but also comes with lots of changes to the Hot Chocolate core. The data package is now completely separated from the core, and no internal APIs are used. Like most of the things in Hot Chocolate, filtering can roughly be broken down into two parts. Schema building and execution. Something we focused on is the new conventions. The goal was to make it easier for users to extend the capabilities of filtering. It is now a lot easier to create custom filters and providers to add new functionality. Both schema building and execution are configurable with conventions.

# Schema Building

Filtering has dedicated input types. `FilterInputType` and `FilterInputType<T>` are extensions of the normal `InputObjectType`. Both filter input types have a similar interface to the normal input type. In addition to `Name`, `Description`, `Directive`, there are a couple of specific descriptors to describe filter capabilities. You can specify fields and operations. There is also `AllowOr` and `AllowAnd`. These two add the special fields needed for these operations. The `FilterInputType` uses the convention for naming and inference of properties. Like the scalar registration on the schema builder, operation types can be bound on the filter convention.

# Execution

To map an incoming GraphQL filter query to the database, Hot Chocolate needs to know how to handle fields and operations. We initially started by having a lookup table. The filter middleware would access this lookup table and search for a matching handler. Since we did a lot of unnecessary work on runtime, we redesigned this to do more of this work at configuration time. During schema initialization, we annotate the matching handler directly from the convention onto the field. For this, we use a new concept call type interceptors. This comes with a few benefits. Firstly, we know during schema creation if all required handlers are registered. In case we do not find a matching handler, we can now fail early and tell the developer what is missing. Secondly, we do not have to do runtime lookups. All handlers are now directly stored on the fields and are available on visitation. We introduced a new concept called type scoping to use more than one filter convention, e.g., MongoDB and SqlServer.

## Type Interceptor

Type interceptors are one of the new shiny features of version 11. To create an interceptor, you have to extend the class `TypeInterceptor` and register it on the schema builder. You can hook into the schema initialization process and make changes across all types or even introduce new once while rewriting the schema. Countless new possibilities come with these new type interceptors. As an example, use-case, we looked at feature flags. Feature flags can be useful in services that are tenant-based. You may want to hide parts of an API for a specific tenant.

The simplest example might be the following one:

> You have an API with two endpoints. One endpoint is for all users of the website (/graphql). The other endpoint is only accessible by administrators (/admin/graphql). The structure of the APIs is the same, the administrators just have access to more fields and mutations.

In previous versions, you would have to create two separate type hierarchies with different types. One for normal users and one for administrators. This would bloat the codebase a lot. With type interceptors and [the new schema creation api](https://chillicream.github.io/hotchocolate/blog/2020/07/16/version-11#configuration-api) this is a lot cleaner.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddTypeInterceptor<RemoveAdminFieldInterceptor>()
        .AddGraphQLServer("admin")
            .AddQueryType<Query>();
}
```

```csharp
public class RemoveAdminFieldInterceptor : TypeInterceptor
{
    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition,
        IDictionary<string, object> contextData)
    {

        if (definition is ObjectTypeDefinition def)
        {
            var fields = (IList<ObjectFieldDefinition>)def.Fields;
            for (var i = fields.Count; i > 0; i--)
            {
                if (fields[i].ContextData.ContainsKey("admin"))
                {
                    fields.RemoveAt(i);
                }
            }
        }
    }
}

public static class ObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor IsAdmin(this IObjectFieldDescriptor descriptor)
    {
        descriptor.Directive("IsAdmin");
        return descriptor;
    }
}

public class ExampleObjectType : ObjectType<Foo> {
    protected override void Configure(IObjectTypeDescriptor<Foo> descriptor){
        descriptor.Field(x => x.AvaiableForAll);
        descriptor.Field(x => x.OnlyForAdmins).IsAdmin();
    }
}
```

## Scoping

With this release, we introduce a concept called schema scoping. As we write handlers from the convention directly on to the fields, we would limit filtering to just one convention. In case we need two conventions we need two fields and therefore two different types. Schema scoping makes it possible to branch of a type hierarchy and create multiple types from the same definition and then later even join the two branches back together. This feature works on the type reference level. Type references now have a scope that can change the type reference identity.
Scoping only really makes sense in combination with a type interceptor. This interceptor picks up a scoped type and then scopes all its dependencies. The type interceptor also has to rename scoped types to avoid name collisions.
Filtering does the same. In case there is only one filter convention registered, you will not see a difference. As soon as you have multiple conventions registered the name of the convention is added to the type name.

## Conventions

Conventions will be the configuration interface for extensions on top of the Hot Chocolate core. In version 11 the convention API has been extended. We introduce the named conventions in this release. This way multiple conventions of the same type can be registered on the Schema.
You may have a filter convention for MongoDB and a filter convention for SqlServer.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
            .AddQueryType<Query>()
            // this will be the default convention as no name is specified
            .AddConvention<IFilterConvention, MongoFilterConvention>()
            .AddConvention<IFilterConvention, FilterConvention>("SqlServer")
            .AddFiltering();
}
```

You can configure the convention when you declare filtering.

```csharp
// pure code first
public class Query {
    [UseFiltering("SqlServer")]
    public IQueryable<Foo> Foos([Service]DbContext context) => context.Foos;
}

//code first
public class Query : ObjectType {
    protected override void Configure(IObjectTypeDescriptor descriptor) {
        descriptor
            .Field<Reslover>(x => x.Foos(default!))
            .UseFiltering("SqlServer");
    }

    public class Resolver {
        public IQueryable<Foo> Foos([Service]DbContext context) => context.Foos;
    }
}
```

## What's next?

We are in the final phase of version 11 development.
In the coming weeks, we will add support for sorting and projections.
The data package is designed for extensibility. There are a few extensions that we will work on. e.g. Filtering for spatial data and a MongoDB provider.
We will as well invest time into documentation and have examples on how to create your own extensions.
There are too many databases to create providers for all of them out of the box. We encourage you, the community, to contribute the extensions you need.
If you are interested, reach out to us in slack in the #contributors channel. We will help you along!
