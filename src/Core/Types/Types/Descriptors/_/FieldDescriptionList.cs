using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public class FieldDescriptionList<T>
        : List<T>
        , IFieldDescriptionList<T>
        where T : FieldDescriptionBase
    {
        public BindingBehavior BindingBehavior { get; set; }
    }
}
