
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HotChocolate.Validation")]
[assembly: InternalsVisibleTo("HotChocolate.Types.CursorPagination")]

// Legacy
[assembly: InternalsVisibleTo("HotChocolate.Types.Filters")]
[assembly: InternalsVisibleTo("HotChocolate.Types.Sorting")]
[assembly: InternalsVisibleTo("HotChocolate.Types.Selections")]

// Tests
[assembly: InternalsVisibleTo("HotChocolate.Types.Filters.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Types.Sorting.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Data.Filters.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Data.Sorting.Tests")]
[assembly: InternalsVisibleTo("HotChocolate.Data.Projections.Tests")]
