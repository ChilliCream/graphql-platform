using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class OutputFieldDescriptionBase
        : FieldDescriptionBase<FieldDefinitionNode>
        , ICanBeDeprecated
    {
        public string DeprecationReason { get; set; }

        public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

        public ICollection<ArgumentDescription> Arguments { get; set; } =
            new List<ArgumentDescription>();
    }
}
