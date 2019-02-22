using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public interface IBindableList<T>
        : IList<T>
        , IReadOnlyList<T>
    {
        BindingBehavior BindingBehavior { get; set; }
    }
}
