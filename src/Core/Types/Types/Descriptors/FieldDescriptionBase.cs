using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class FieldDescriptionBase
        : TypeDescriptionBase
    {
        protected FieldDescriptionBase() { }

        public TypeReference TypeReference { get; set; }

        public bool Ignored { get; set; }

        public bool? IsNullable { get; set; }

        public bool? IsElementNullable { get; set; }
    }
}
