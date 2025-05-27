using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class SortEnumValueConfiguration : EnumValueConfiguration
{
    public ISortOperationHandler Handler { get; set; } = null!;

    public int Operation { get; set; }

    public object Value
    {
        get => RuntimeValue!;
        set => RuntimeValue = value;
    }
}
