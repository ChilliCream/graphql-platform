using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class FieldDescriptionList<T>
        : List<T>
        , IFieldDescriptionList<T>
        where T : FieldDescriptionBase
    {
        public BindingBehavior BindingBehavior { get; set; }
    }
}
