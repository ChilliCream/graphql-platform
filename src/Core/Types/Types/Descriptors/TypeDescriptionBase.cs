using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class TypeDescriptionBase
    {
        protected TypeDescriptionBase() { }

        public string Name { get; set; }

        public string Description { get; set; }

        public BindingBehavior BindingBehavior { get; set; } =
            BindingBehavior.Implicit;

        public List<DirectiveDescription> Directives { get; set; } =
            new List<DirectiveDescription>();

        public Dictionary<string, object> ContextData { get; } =
            new Dictionary<string, object>();
    }
}
