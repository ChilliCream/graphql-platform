---
title: "Get started with Hot Chocolate"
---

import { ApiChoiceTabs } from "../../components/mdx/api-choice-tabs"
import { InputChoiceTabs } from "../../components/mdx/input-choice-tabs"

TODO

# Setup

There are two ways to setup a Hot Chocolate GraphQL server. Either you use our official template or you integrate Hot Chocolate into an existing project manually. While the from scratch approach certainly takes a bit longer, ultimately the setup is really straight forward and fast in both cases.

Keep reading if you want to use our template or jump ahead to the [from scratch section](#from-scratch).

## Using our template

We offer some templates for Hot Chocolate to help you get a GraphQL server up and running in a matter of seconds.

#### 1. Install the templates

The `HotChocolate.Templates` collection can be installed like the following.

```bash
dotnet new -i HotChocolate.Templates
```

These templates are kept up to date by us with the latest .NET and Hot Chocolate features. We are for example making use of _Implicit Usings_ added with .NET 6 to provide the most common Hot Chocolate namespaces implicitly.

> Note: Our templates have to be updated manually. To update just re-execute the above command. We recommend doing so every time Hot Chocolate releases a new major version.

#### 2. Create a new project using a template

Once you have installed our templates you can use them to bootstrap your next ASP.NET Core project with Hot Chocolate.

<InputChoiceTabs>
<InputChoiceTabs.CLI>

```bash
dotnet new graphql -n Demo
```

This will create a new directory called "Demo" containing your project's files.

</InputChoiceTabs.CLI>
<InputChoiceTabs.VisualStudio>

TODO

</InputChoiceTabs.VisualStudio>
</InputChoiceTabs>

After you have successfully created the project you can go ahead and open it in your favorite Code Editor.

And this is it - you have successfully setup a Hot Chocolate GraphQL server! 🚀

[Lets explore how you can execute your first GraphQL query](#executing-a-query)

## From scratch

If you do not want to use the template or you have to integrate Hot Chocolate into an existing ASP.NET Core application, you can setup a functioning GraphQL server in a few simple steps. If you have already created an ASP.NET Core project you can skip step 1.

#### 1. Create a new ASP.NET Core project

We start of by creating a new ASP.NET Core project.

<InputChoiceTabs>
<InputChoiceTabs.CLI>

```bash
dotnet new web -n Demo
```

This will create a new directory called "Demo" containing your project's files.

</InputChoiceTabs.CLI>
<InputChoiceTabs.VisualStudio>

<!-- todo: verify template name -->

In Visual Studio you can create a new ASP.NET Core project using the "Web" template.

[Learn how you can create a new project within Visual Studio](https://docs.microsoft.com/visualstudio/ide/create-new-project)

</InputChoiceTabs.VisualStudio>
</InputChoiceTabs>

After you have successfully created the project you can go ahead and open it in your favorite Code Editor.

#### 2. Add the HotChocolate.AspNetCore package

This package includes everything that's needed to get your GraphQL server up and running.

<InputChoiceTabs>
<InputChoiceTabs.CLI>

```bash
dotnet add package HotChocolate.AspNetCore
```

</InputChoiceTabs.CLI>
<InputChoiceTabs.VisualStudio>

You can add the `HotChocolate.AspNetCore` package using the NuGet Package Manager within Visual Studio.

[Learn how you can use the NuGet Package Manager to install a package](https://docs.microsoft.com/nuget/quickstart/install-and-use-a-package-in-visual-studio#nuget-package-manager)

</InputChoiceTabs.VisualStudio>
</InputChoiceTabs>

#### 3. Defining the schema

Next, we want to create a GraphQL schema. The GraphQL schema defines which data we expose and how consumers can interact with said data.

For starters we can define two object types (models) that we want to expose through our schema.

```csharp
public class Author
{
    public string Name { get; set; }\
}

public class Book
{
    public string Title { get; set; }

    public Author Author { get; set; }
}
```

With these two classes we have a nice and simple model that we can use to build our GraphQL schema.

#### 4. Adding a Query type

Now that we have defined our models, we need to

```csharp
public class Query
{
    public Book GetBook() =>
        new Book
        {
            Title = "C# in depth.",
            Author = new Author
            {
                Name = "Jon Skeet"
            }
        };
}
```

#### 5. Adding GraphQL services

Next, we need to add the services required by Hot Chocolate to operate a GraphQL server to our Dependency Injection container.

<ApiChoiceTabs>
<ApiChoiceTabs.MinimalApis>

TODO

</ApiChoiceTabs.MinimalApis>
<ApiChoiceTabs.Regular>

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>();
}
```

</ApiChoiceTabs.Regular>
</ApiChoiceTabs>

The `AddGraphQLServer` returns an `IRequestExecutorBuilder` which is the main way to configure our GraphQL server. In the above example we are specifying the Query type that should be exposed by our GraphQL server.

#### 6. Mapping the GraphQL endpoint

Now that we've added the necessary services, we need to expose our GraphQL server at an endpoint. Hot Chocolate comes with an ASP.NET Core middleware that is used to serve up the GraphQL server.

<ApiChoiceTabs>
<ApiChoiceTabs.MinimalApis>

TODO

</ApiChoiceTabs.MinimalApis>
<ApiChoiceTabs.Regular>

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGraphQL();
    });
}
```

</ApiChoiceTabs.Regular>
</ApiChoiceTabs>

And this is it - you have successfully setup a Hot Chocolate GraphQL server! 🚀

# Executing a query

In order to isue a query against your newly built GraphQL server, we first have to run it.

<InputChoiceTabs>
<InputChoiceTabs.CLI>

```bash
dotnet run
```

</InputChoiceTabs.CLI>
<InputChoiceTabs.VisualStudio>

TODO

</InputChoiceTabs.VisualStudio>
</InputChoiceTabs>

If you have setup everything so far correctly you should be able to naviagte to <a href="http://localhost:5000/graphql" target="_blank" rel="noopener noreferrer">http://localhost:5000/graphql</a> and be greeted by our GraphQL IDE [Banana Cake Pop](/docs/bananacakepop)

![GraphQL IDE](../../images/get-started-bcp.png)

![GraphQL IDE execute Query](../../images/get-started-bcp-query.png)

[Learn more about the features of Banana Cake Pop](/docs/bananacakepop)

# Additional resources

Now that you've setup a basic GraphQL server using Hot Chocolate, what should be your next steps?

If this is your first time using GraphQL, we recommend [this guide](https://graphql.org/learn/) that walks you through the basic concepts of GraphQL.

If you want to get an overview of Hot Chocolate's features, we recommend reading the _Overview_ pages in each section of the documentation. They can be found in the sidebar to your left.

For a guided tutorial that explains how you can setup your GraphQL server beyond this basic example, checkout [our workshop](https://github.com/ChilliCream/graphql-workshop). Here we will dive deeper into several topics around Hot Chocolate and GraphQL in general.

You can also jump straight into our documentation and learn more about<br/>[Defining a GraphQL schema](/docs/hotchocolate/defining-a-schema).

<!--


2. Open your browser and head over to `http://localhost:5000/graphql` to open our built-in GraphQL IDE [Banana Cake Pop](/docs/bananacakepop).

![GraphQL IDE](../../images/get-started-bcp.png)

3. Next, click on the `Book` icon in the left-hand navigation bar to explore the server's GraphQL schema. If this is the first time you are running the demo, you will need to enter `http://localhost:5000/graphql` as the schema endpoint URI. In the schema explorer, we can see that we have one query root field exposed. By clicking on the field, we can drill into the schema structure.

![GraphQL IDE Schema Explorer](../../images/get-started-bcp-schema-explorer.png)

4. Head back to the query tab and execute your first GraphQL query by clicking the play button.

```graphql
{
  book {
    title
    author {
      name
    }
  }
}
```

![GraphQL IDE Execute Query](../../images/get-started-bcp-query.png)

# Summary

In this guide, we have learned how to set up a simple GraphQL server project and define a GraphQL schema with .NET.

Moreover, we explored our GraphQL schema with our GraphQL IDE Banana Cake Pop and executed a simple query to test our server.

If you want to dive deeper, you can start with our [GraphQL tutorial](https://github.com/ChilliCream/graphql-workshop) to get into several topics around GraphQL and Hot Chocolate.

Further, you can learn more about defining GraphQL schemas in .NET [here](/docs/hotchocolate/defining-a-schema). -->
