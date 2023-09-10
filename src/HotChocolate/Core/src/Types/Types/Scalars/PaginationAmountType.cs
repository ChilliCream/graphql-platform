#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Provodes legacy support for paging.
/// </summary>
internal sealed class PaginationAmountType : IntType 
{
    public PaginationAmountType()
        : base(
            ScalarNames.PaginationAmount,
            null,
            int.MinValue,
            int.MaxValue,
            BindingBehavior.Explicit)
    {
    }
}
