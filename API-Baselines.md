# API Baselines

This document contains information regarding API baseline files and how to work with them.

## Files

Each project contains two files tracking the public API surface of this project.

### PublicAPI.Shipped.txt

This file contains APIs that were released in the last major version.

This file should only be modified after a major release by the maintainers and should never be modified otherwise. There is a [script](#scripts) to perform this automatically.

### PublicAPI.Unshipped.txt

This file contains API changes since the last major version.

## Scenarios

There are three types of public API changes that need to be documented.

### New APIs

A new entry needs to be added to the `PublicAPI.Unshipped.txt` file for a new API. For example:

```
#nullable enable
Microsoft.AspNetCore.Builder.NewApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

### Removed APIs

A new entry needs to be added to the `PublicAPI.Unshipped.txt` file for a removed API. For example:

```
#nullable enable
*REMOVED*Microsoft.Builder.OldApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

### Updated APIs

Two new entries need to be added to the `PublicAPI.Unshipped.txt` file for an updated API. One to remove the old API and one for the new API. For example:

```
#nullable enable
*REMOVED*Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string!
Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string?
```

## New projects

TODO

## Scripts

There are three scripts to help you manage the `PublicAPI.*.txt` files. They can be found [here](./scripts).

### mark-api-shipped.ps1

This transfers all changes in the `PublicAPI.Unshipped.txt` to the `PublicAPI.Shipped.txt` files. It also takes care of removing lines marked with `*REMOVE*` (removals of APIs).

### display-unshipped-api.ps1

This will output the contents of all `PublicAPI.Unshipped.txt` files throughout the project.

### diff-shipped-api.ps1

This shows all changes of `PublicAPI.Shipped.txt` files between git refs. Example: `diff-shipped-api.ps1 -from 11.0.0 -to 12.0.0`
