# API Baselines

This document contains information regarding API baseline files and how to work with them.

## Files

Each project contains two files that track the public API surface that has been previously shipped and the currently un-shipped changes to this API surface.

### PublicAPI.Shipped.txt

This file contains APIs that were released in the last major version.
This file should only be modified after a major release by the maintainers and should never be modified otherwise.

### PublicAPI.Unshipped.txt

This file contains new APIs since the last major version. Steps for updating this file are found in [Steps for adding and updating APIs](#steps-for-adding-and-updating-apis).

### Scripts

TODO

## Scenarios

There are three scenarios related to public API changes that need to be documented.

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

## Steps for adding and updating APIs

TODO

## New projects

TODO
