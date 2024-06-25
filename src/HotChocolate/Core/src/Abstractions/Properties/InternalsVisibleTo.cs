using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HotChocolate.CostAnalysis")]

// legacy
[assembly: InternalsVisibleTo("HotChocolate.Types")]
[assembly: InternalsVisibleTo("HotChocolate.Execution")]
[assembly: InternalsVisibleTo("HotChocolate.Validation")]
[assembly: InternalsVisibleTo("HotChocolate.Stitching")]

// tests
[assembly: InternalsVisibleTo("HotChocolate.Abstractions.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Core.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Types.Tests")]
