using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortEnumTypeDefinition
        : EnumTypeDefinition,
          IHasScope
    {
        public string? Scope { get; set; }
    }
}
