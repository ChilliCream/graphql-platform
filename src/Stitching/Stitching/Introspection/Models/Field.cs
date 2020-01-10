using System.Collections.Generic;

namespace HotChocolate.Stitching.Introspection.Models
{
    internal class Field
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<InputField> Args { get; set; }
        public TypeRef Type { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationReason { get; set; }
    }
}
