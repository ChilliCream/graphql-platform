using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class FieldDescriptionBase
        : TypeDescriptionBase
    {
        protected FieldDescriptionBase() { }

        public TypeReference TypeReference { get; set; }
    }
}
