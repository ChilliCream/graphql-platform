using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ArgumentDefinition
        : FieldDefinitionBase<InputValueDefinitionNode>
    {
        private List<IInputValueFormatter>? _formatters;

        public IValueNode? DefaultValue { get; set; }

        public object? NativeDefaultValue { get; set; }

        public ParameterInfo? Parameter { get; set; }

        public IList<IInputValueFormatter> Formatters =>
            _formatters ??= new List<IInputValueFormatter>();

        internal IReadOnlyList<IInputValueFormatter> GetFormatters()
        {
            if (_formatters is null)
            {
                return Array.Empty<IInputValueFormatter>();
            }

            return _formatters;
        }

        internal void CopyTo(ArgumentDefinition target)
        {
            base.CopyTo(target);

            target._formatters = _formatters;
            target.DefaultValue = DefaultValue;
            target.NativeDefaultValue = NativeDefaultValue;
            target.Parameter = Parameter;
        }
    }
}
