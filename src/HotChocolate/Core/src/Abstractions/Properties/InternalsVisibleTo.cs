using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HotChocolate.CostAnalysis")]

// legacy
[assembly: InternalsVisibleTo("HotChocolate.Types")]
[assembly: InternalsVisibleTo("HotChocolate.Execution")]
[assembly: InternalsVisibleTo("HotChocolate.Validation")]

// tests
[assembly: InternalsVisibleTo("HotChocolate.Abstractions.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Core.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Types.Tests")]
