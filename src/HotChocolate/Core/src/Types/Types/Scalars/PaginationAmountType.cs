#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Provides legacy support for paging.
/// </summary>
[method: ActivatorUtilitiesConstructor]
internal sealed class PaginationAmountType() 
    : IntType(ScalarNames.PaginationAmount);
