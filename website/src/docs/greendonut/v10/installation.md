---
title: Installation
---

Before we can start we have to install the package via _NuGet_. There are many
ways to install a _NuGet_ package but we focus here on the most common _CLI_
commands for both _.Net Core_ and _.Net Framework_.

For _.NET Core_ we use the `dotnet` _CLI_, which is perhaps the preferred way
doing this.

```powershell
dotnet add package GreenDonut
```

And for _.NET Framework_ we still use the following line.

```powershell
Install-Package GreenDonut
```

People who prefer a UI to install packages might want to use the
_NuGet Package Manager_, which is provided by _Visual Studio_.
