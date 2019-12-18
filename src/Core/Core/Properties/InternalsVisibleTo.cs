using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HotChocolate.Core.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Validation.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// this is temporary until we reworked the variable coercion #1274
[assembly: InternalsVisibleTo("HotChocolate.Stitching")]
