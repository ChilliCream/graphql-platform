---
title: "Operation Reporting"
--- 

![Image](images/operation-reporting-1.png)

Banana Cake Pop's Operation Reporting feature provides comprehensive insights into the GraphQL operations executed on your server. This functionality is essential for maintaining visibility over server activities, including both persisted and executed operations. By leveraging Operation Reporting, developers and system administrators can gain a clearer understanding of what operations are executed and available on the server.

# Enabling Operation Reporting

Operation Reporting is an integrated feature in Banana Cake Pop and is enabled by default when using Banana Cake Pop services. To integrate these services into your project, the [`BananaCakePop.Services`](https://www.nuget.org/packages/BananaCakePop.Services/) NuGet package must be added.

To install the Banana Cake Pop services, run the following command in your project's root directory:

```bash
dotnet add package BananaCakePop.Services
```

After installing the package, you need to configure the services in your startup class. Below is a sample implementation in C#:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddBananaCakePopServices(x =>
        {
            x.ApiKey = "<<your-api-key>>";
            x.ApiId = "QXBpCmc5NGYwZTIzNDZhZjQ0NjBmYTljNDNhZDA2ZmRkZDA2Ng==";
            x.Stage = "dev";
        });
}
```

> **Tipp: Using Environment Variables**
>
> Alternatively, you can set the required values using environment variables. This method allows you to call `AddBananaCakePopServices` without explicitly passing parameters.
>- `BCP_API_KEY` maps to `ApiKey`
>- `BCP_API_ID` maps to `ApiId`
>- `BCP_STAGE` maps to `Stage`
>```csharp
>public void ConfigureServices(IServiceCollection services)
>{
>    services
>        .AddGraphQLServer()
>        .AddQueryType<Query>()
>        .AddBananaCakePopServices() 
>}
>```
>In this setup, the API key, ID, and stage are set through environment variables.

# Viewing Reported Operations

Once Operation Reporting is enabled and configured, all GraphQL operations processed by your server will be reported to Banana Cake Pop. These operations can be viewed and analyzed in the `Operations` tab within the Banana Cake Pop interface.

![Image](images/operation-reporting-2.png)
1. Click the `Operations` tab in Banana Cake Pop to view the list of reported operations.
2. The name of the executed operation.
3. Click on `V` to expand the operation and view the details.
4. The document id of the persisted operation.
