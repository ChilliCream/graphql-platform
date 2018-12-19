using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class TypeDescriptionBase
    {
        protected TypeDescriptionBase() { }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<DirectiveDescription> Directives { get; set; } =
            new List<DirectiveDescription>();
    }
}
