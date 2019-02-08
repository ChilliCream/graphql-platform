using System.Collections.Generic;

namespace HotChocolate.Stitching.Introspection.Models
{

    internal class Directive
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<InputField> Args { get; set; }
        public bool OnOperation { get; set; }
        public bool OnFragment { get; set; }
        public bool OnField { get; set; }
    }
}
