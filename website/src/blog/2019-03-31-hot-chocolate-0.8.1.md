---
path: "/blog/2019/03/31/hot-chocolate-0.8.1"
date: "2019-03-31"
title: "GraphQL - Hot Chocolate 0.8.1"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we release version 8.1 (0.8.1) of Hot Chocolate. This release brings improvements and bug fixes to the current version 8 release.

## Instrumentation

One focus of this release was to open up our diagnostic events to be used by developers. When we started thinking about how Hot Chocolate should provide information about its inner workings to users of the library, we opted against using one specific logging framework.

Instead we have looked at what Microsoft was doing in ASP.Net core and other components with diagnostic sources. Diagnostic sources let us create events that have non-serializable payloads.

This means that we can provide an event that gives full access to our context objects like the `IQueryContext` or the `IResolverContext`.

This enables you to add your own logger to your GraphQL server and grab exactly the information from the Hot Chocolate diagnostic events that you need to make your tracing solution work.

In order to read more on this subject checkout our blog: [Tracing with Hot Chocolate](2019-03-19-logging-with-hotchocolate.md) or head over to our [documentation](https://hotchocolate.io/docs/instrumentation).

## Stitching Refinements

One new feature that is now available in the stitching layer is support of error filters. This means that you can now write error filters like on a local schema and transform or enrich query errors that were extracted from remote queries.

In order to make it easier to use error filters we have changed the error structure of remote errors and provide the original error object as an extension property:

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

We also refined the default rewrite logic so that errors in most cases will now be correctly associated with the causing field.

## Bug Fixes

`DateTime` scalars are now correctly handled in the stitching layer, with version 8 we had some issues when `DateTime` scalars were provided through variables.

For more information on what other bugs we fixed head over to our [changelog](https://github.com/ChilliCream/hotchocolate/blob/master/CHANGELOG.md).

## Version 9 Development

We have made a lot of headway with the new type system that is coming with version 9. Also, we are working on the `@defer` directive at the moment. We will give a more detailed update on the next major version in a sperate blog post. Version 9 is really shaping up to become our biggest release so far.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
