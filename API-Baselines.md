# API Baselines

This document contains information regarding API baseline files and how to work with them.

## Files

Each project contains two files tracking the public API surface of said project. These files are used by the [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers) roslyn analyzer as a reference to what the public API surface looked like previously.

### PublicAPI.Shipped.txt

This file contains APIs that were released in the last major version.

This file should only be modified after a major release by the maintainers and should never be modified otherwise. There is a [script](#scripts) to perform this automatically.

### PublicAPI.Unshipped.txt

This file contains API changes since the last major version.

## Types of public API changes

There are three types of public API changes that need to be documented.

### New APIs

This case will be indicated by an error/warning like the following:

```
RS0016: Symbol 'X' is not part of the declared API
```

It can be resolved by adding the new symbol to the `PublicAPI.Unshipped.txt` file:

```
#nullable enable
Microsoft.AspNetCore.Builder.NewApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

This change can be performed automatically by your IDE.

> Note: Currently not every IDE supports Code-Fixes provided by a Roslyn Analyzer. Visual Studio Code for example does not at the moment - Visual Studio 2019 does.

### Removed APIs

This case will be indicated by an error/warning like the following:

```
RS0017: Symbol 'X' is part of the declared API, but is either not public or could not be found
```

It can be resolved by adding the removed symbol to the `PublicAPI.Unshipped.txt` file:

```
#nullable enable
*REMOVED*Microsoft.Builder.OldApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

This change needs to be done manually. Copy the relevant line from `PublicAPI.Shipped.txt` into `PublicAPI.Unshipped.txt` and place `*REMOVED*` in front of it.

### Updated APIs

Two new entries need to be added to the `PublicAPI.Unshipped.txt` file for an updated API. One to remove the old API and one for the new API. For example:

```
#nullable enable
*REMOVED*Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string!
Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string?
```

The removed case needs to be done manually as explained [here](#removed-apis).

## Ignoring projects

Projects ending in `.Tests` or `.Resources` are ignored per default.

If you need to manually ignore a project, include the following in its `.csproj` file:

```xml
<PropertyGroup>
    <AddPublicApiAnalyzers>false</AddPublicApiAnalyzers>
</PropertyGroup>
```

## New projects

The two text files mentioned above need to be added to each new project.

There is a template file called `PublicAPI.empty.txt` in the `scripts` directory that can be copied over into a new project.

```sh
DIR="<new-project-folder>"
cp scripts/PublicAPI.empty.txt "src/$DIR/PublicAPI.Shipped.txt"
cp scripts/PublicAPI.empty.txt "src/$DIR/PublicAPI.Unshipped.txt"
```

## Pipeline Job

A PR to `main` will trigger a Pipeline job that runs this analyzer. Contrary to the development environment, where the analyzer only reports warnings, here it throws an error (This behavior is only true if the `RequireDocumentationOfPublicApiChanges` environment variable is set). This is done to enforce highlighting of public API changes.

The job is defined as part of [this Pipeline definition](./.devops/azure-pipelines.test-pr-hotchocolate.yml). It can be easily disabled by adding `condition: false` to the `CheckPublicApi` section or commenting out the section.

## Scripts

There are three scripts to help you manage the `PublicAPI.*.txt` files. They can be found [here](./scripts).

### mark-api-shipped.ps1

This transfers all changes in the `PublicAPI.Unshipped.txt` to the `PublicAPI.Shipped.txt` files.

It also takes care of removing lines marked with `*REMOVE*` (removals of APIs).

### display-unshipped-api.ps1

This will output the contents of all `PublicAPI.Unshipped.txt` files throughout the project.

If we only want to see the breaking changes, we can add the `breaking` flag:

```sh
display-unshipped-api.ps1 -breaking $true
```

### diff-shipped-api.ps1

This shows all changes of `PublicAPI.Shipped.txt` files between git refs. Tags, commit hashes and branch names are supported.

Example:

```sh
diff-shipped-api.ps1 -from 11.0.0 -to 12.0.0
```
