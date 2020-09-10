using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortEnumValueDefinition : EnumValueDefinition
    {
        public ISortOperationHandler Handler { get; set; } = default!;

        public int Operation { get; set; }

        public new object Value
        {
            get => base.Value!;
            set => base.Value = value;
        }
    }
}
