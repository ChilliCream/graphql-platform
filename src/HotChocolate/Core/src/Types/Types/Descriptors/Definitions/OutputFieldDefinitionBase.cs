using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable  enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class OutputFieldDefinitionBase
        : FieldDefinitionBase<FieldDefinitionNode>
        , ICanBeDeprecated
    {
        private List<ArgumentDefinition>? _arguments;

        public string? DeprecationReason { get; set; }

        public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

        public IList<ArgumentDefinition> Arguments => _arguments ??= new List<ArgumentDefinition>();

        public IReadOnlyList<ArgumentDefinition> GetArguments()
        {
            if (_arguments is null)
            {
                return Array.Empty<ArgumentDefinition>();
            }

            return _arguments;
        }
    }
}
