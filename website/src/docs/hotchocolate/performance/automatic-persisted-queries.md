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
dotnet package add HotChocolate.PersistedQueries.InMemory
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
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseActivePersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```
