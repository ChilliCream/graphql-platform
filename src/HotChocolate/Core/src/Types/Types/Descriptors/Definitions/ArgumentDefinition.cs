using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ArgumentDefinition : FieldDefinitionBase<InputValueDefinitionNode>
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

        internal void MergeInto(ArgumentDefinition target)
        {
            base.MergeInto(target);

            if (_formatters is { Count: > 0 })
            {
                target._formatters ??= new List<IInputValueFormatter>();
                target._formatters.AddRange(_formatters);
            }

            if (DefaultValue is not null)
            {
                target.DefaultValue = DefaultValue;
            }

            if (NativeDefaultValue is not null)
            {
                target.NativeDefaultValue = NativeDefaultValue;
            }

            if (Parameter is not null)
            {
                target.Parameter = Parameter;
            }
        }
    }
}
