using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IBindableList<T>
        : IList<T>
        , IReadOnlyList<T>
    {
        BindingBehavior BindingBehavior { get; set; }

        void AddRange(IEnumerable<T> items);
    }
}
