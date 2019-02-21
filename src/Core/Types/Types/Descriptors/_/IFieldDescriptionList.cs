using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IFieldDescriptionList<T>
        : IList<T>
        , IReadOnlyList<T>
        where T : FieldDescriptionBase
    {
        BindingBehavior BindingBehavior { get; set; }
    }
}
