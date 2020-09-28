---
title: .NET CLI
---

Because we love the new Microsoft .NET command line interface we have created a Hot Chocolate server template that lets you setup a new GraphQL server in seconds.

Here is how to install the Hot Chocolate template into your dotnet CLI:

```bash
dotnet new -i HotChocolate.Templates.Server
```

This will pull down the Hot Chocolate server template from nuget.org and integrate it into your dotnet CLI.

_Since the .NET SDKs are installed side by side the above command will only install the template into the current SDK. If you upgrade your SDK version you will need to re-run this command to install the template into your new SDK._

Moreover, if you want to update your template to a newer version first uninstall the current template version.

```bash
dotnet new -u HotChocolate.Templates.Server
dotnet new -i HotChocolate.Templates.Server
```

In order to create a new GraphQL server that already contains the hello world example just run the following command:

```bash
dotnet new graphql -n MyProjectName
```

If you also love to develop .NET in Visual Studio Code just run the following commands to get you started.

```bash
mkdir graphql-demo
cd graphql-demo
dotnet new graphql
code .
```

Have fun getting started.
