using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL argument type.
    /// </summary>
    public class ArgumentDefinition : FieldDefinitionBase<InputValueDefinitionNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
        /// </summary>
        public ArgumentDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
        /// </summary>
        public ArgumentDefinition(
            NameString name,
            string? description = null,
            ITypeReference? type = null,
            IValueNode? defaultValue = null,
            object? runtimeDefaultValue = null)
        {
            Name = name;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
            RuntimeDefaultValue = runtimeDefaultValue;
        }

        private List<IInputValueFormatter>? _formatters;

        public IValueNode? DefaultValue { get; set; }

        public object? RuntimeDefaultValue { get; set; }

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
            target.RuntimeDefaultValue = RuntimeDefaultValue;
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

            if (RuntimeDefaultValue is not null)
            {
                target.RuntimeDefaultValue = RuntimeDefaultValue;
            }

            if (Parameter is not null)
            {
                target.Parameter = Parameter;
            }
        }
    }
}
