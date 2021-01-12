---
title: "Automatic persisted queries"
---

```
dotnet new graphql -n PersistedQueries
```

```
dotnet package add HotChocolate.AspNetCore
```

```
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseActivePersistedQueryPipeline();
}
```

```
services
    .AddRouting()
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UseActivePersistedQueryPipeline()
    .AddInMemoryQueryStorage();
```
