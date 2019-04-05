using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class BindableList<T>
        : List<T>
        , IBindableList<T>
    {
        public BindingBehavior BindingBehavior { get; set; }
    }
}
