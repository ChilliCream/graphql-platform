---
path: "/blog/2019/03/04/hot-chocolate-0.8.0"
date: "2019-03-04"
title: "GraphQL - Hot Chocolate 0.8.0"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we are releasing Hot Chocolate version 8 (0.8.0) which mainly focused on schema stitching and brings our stitching layer to a whole new level.

## Schema Stitching

Since, my last blog post we were heavily at work ironing out bugs and making schema stitching easier.

Now, with the release finished, schema stitching with ASP.Net core has become super easy and feels quite nice to use.

Head over to our new documentation for [schema stitching](https://hotchocolate.io/docs/stitching).

## Voyager

With version 8 we now provide a GraphQL Voyager middleware. GraphQL Voyager is a nice schema explorer that can be useful during development time. If you want to know more about GraphQL Voyager head over to their [GitHub repo](https://github.com/APIs-guru/graphql-voyager).

## Authorization

Also, with version 8 we have invested some time to smooth out the `@authorize`-directive.

The `@authorize`-directive does now mirror almost the behavior of the authorize directive.

In contrast to the ASP.Net attribute we can specify the directive on field definitions and thereby have a fine-grained control over what data we want to give access to.

If you are using ASP.Net core then you can use authorization-policies with your `@authorize`-directive giving you even more control over your data.

Head over to our [authorization documentation](https://hotchocolate.io/docs/authorization) to learn more.

## Version 9

We have already started on quite a few areas for our next release. I can tell you already that, version 9 will be big.

### Type System

The most requested features from users of our API at the moment is to open up the type system. In the beginning we did keep a lot of extension points internal in order to give us some more time to figure out how to make certain areas extendable.

With version 9 we will reinvent the type system. Do not fret, there will be no breaking changes to the public APIs since the APIs that we are changing and making public are currently internal.

With version 9 you will be able to create your own base classes that expose your own descriptors to the users. This will allow for instance to introduce prisma-like filter APIs, or dynamic types that generate members on the base of other schema types.

### Prisma-like Filtering for IQueryable

On top of the new type system we will add new filter types that will allow you to configure filter and sorting inputs that can be used with the paging middleware. If you never heard of Prisma then head over to their web page and checkout their approach to filtering and sorting:

[Prisma](https://www.prisma.io/docs/prisma-graphql-api/reference/queries-qwe1/)

### Advanced Relay Support

With version 9 creating relay compliant schemas will be as easy as eating pie. You will no longer be bothered handling schema unique identifiers, since Hot Chocolate will do all of that for you. Also, the node field on the `Query` type will be automatically integrated. So, what we are doing here is removing boilerplate code for you so that you can focus on implementing a great API without having to worry about the relay server spec details.

### Subscription Stitching

We originally envisioned this feature for version 8 but moved this one into version 9 due to the fact that we needed some changes in the type system to handle it. This will make the schema stitching even more complete.

### Relay Schema Stitching

With the schema stitching version 8 you have to handle the node field on your own when you stitch multiple relay compliant schemas together. With version 9 we will keep track which id belongs to which remote schema and from which remote schema we have to fetch the data.

### Hot Chocolate UI

GraphQL is really awesome, but we are really not happy with the tooling situation. As of now we support GraphiQL, Playground and Voyager for Hot Chocolate, but none of these is a complete solution.

We have started some time ago to create a new developer tool for GraphQL that will replace all of these. We did not base our new UI on GraphiQL since we want to achieve more and create something unique. Look for instance at the tooling around rest, with _Postman_ developers have quite a good tool that enables them to do a lot.

The _Hot Chocolate UI_ will be a developer focused tool that will be able to replace all the GraphQL UIs out there. It already is my favorite tool and we cannot wait to show you the first preview versions of it.

By the way, we are still looking for a cool new chillicream compliant name like Hot Chocolate or Green Donut. So, if you have any cool or funny ideas head over to our slack channel

### GraphQL Compatibility Acceptance Tests

With version 8 we have started to invest in the GraphQL Compatibility Acceptance Tests and plan to have them fully implemented and integrated with version 11. This does not mean that we wait until version 11 to use them. Already now we are able to generate some of the test cases. Hopefully, we will have all the parser tests integrated with version 9. This subject is an ongoing effort and we will keep you posted on this one.

For more information on GraphQL Cats visit their [GitHub repository](https://github.com/graphql-cats/graphql-cats).

### Versioning

With version 9 we will change our versioning and follow the example of react in swapping the leading zero with the nine. So, the version number of version 9 will actually be 9.0.0.

## Version 10

With version 10 is in the early planning stages, we will build on the new type system and introduce two new services that will turn Hot Chocolate from a simple server into a GraphQL platform.

### Schema Registry

The schema registry will keep track of the schemas in your company. Moreover, with version 10 of our new `Hot Chocolate UI` you will be able to configure your GraphQL gateways with drag&drop. This means you will be able to stitch schemas together with an awesome UI and deploy new stitched schemas in seconds.

### Performance and Schema Warehouse

The second service will collect performance data from all your schemas. You will be able to analyze with the `Hot Chocolate UI` how good or bad you GraphQL servers are performing, which queries are the most used or which queries use the most resources. Furthermore, you will be able to drill into the query tracing results and see which resolvers are performing well or which resolvers are causing issues.

## Wrapping things up

We are planning around four to six weeks for version 9 with the first previews coming out in around two weeks.

We will really start hammering out the details on version 9 in the next three weeks.

If you have ideas or suggestions pleas head over to our slack channel and join the discussion.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
