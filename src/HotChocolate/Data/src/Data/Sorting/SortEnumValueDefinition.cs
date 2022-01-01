using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public class SortEnumValueDefinition : EnumValueDefinition
{
    public ISortOperationHandler Handler { get; set; } = default!;

    public int Operation { get; set; }

    public object Value
    {
        get => base.RuntimeValue!;
        set => base.RuntimeValue = value;
    }
}
