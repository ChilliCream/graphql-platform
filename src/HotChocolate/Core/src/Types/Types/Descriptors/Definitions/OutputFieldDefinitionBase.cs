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

        public IList<ArgumentDefinition> Arguments =>
            _arguments ??= new List<ArgumentDefinition>();

        public IReadOnlyList<ArgumentDefinition> GetArguments()
        {
            if (_arguments is null)
            {
                return Array.Empty<ArgumentDefinition>();
            }

            return _arguments;
        }

        protected void CopyTo(OutputFieldDefinitionBase target)
        {
            base.CopyTo(target);

            if (_arguments is not null)
            {
                target._arguments = new List<ArgumentDefinition>();

                foreach (ArgumentDefinition argument in _arguments)
                {
                    var newArgument = new ArgumentDefinition();
                    argument.CopyTo(newArgument);
                    target._arguments.Add(newArgument);
                }
            }
        }
    }
}
