using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Pipeline.Complexity
{
    /// <summary>
    /// The context to calculate a field complexity.
    /// </summary>
    public readonly ref struct ComplexityContext
    {
        private readonly IVariableValueCollection _valueCollection;

        /// <summary>
        /// Creates a new instance of <see cref="ComplexityContext" />
        /// </summary>
        public ComplexityContext(
            IOutputField field,
            FieldNode selection,
            CostDirective? cost,
            int fieldDepth,
            int nodeDepth,
            int childComplexity,
            int defaultComplexity,
            IVariableValueCollection valueCollection)
        {
            Field = field;
            Selection = selection;
            Complexity = cost?.Complexity ?? defaultComplexity;
            ChildComplexity = childComplexity;
            Multipliers = cost?.Multipliers ?? Array.Empty<MultiplierPathString>();
            DefaultMultiplier = cost?.DefaultMultiplier;
            FieldDepth = fieldDepth;
            NodeDepth = nodeDepth;
            _valueCollection = valueCollection;
        }

        /// <summary>
        /// Gets the field for which the complexity is calculated.
        /// </summary>
        public IOutputField Field { get; }

        /// <summary>
        /// Gets the field selection that references the field in the query.
        /// </summary>
        public FieldNode Selection { get; }

        /// <summary>
        /// Gets the field`s base complexity.
        /// </summary>
        /// <value></value>
        public int Complexity { get; }

        /// <summary>
        /// Gets the calculated complexity of all child fields.
        /// </summary>
        public int ChildComplexity { get; }

        /// <summary>
        /// Gets the multiplier argument names.
        /// </summary>
        /// <value></value>
        public IReadOnlyList<MultiplierPathString> Multipliers { get; }

        /// <summary>
        /// Gets the default multiplier value that is used when no 
        /// multiplier argument has a value.
        /// </summary>
        public int? DefaultMultiplier { get; }

        /// <summary>
        /// Gets the field depth in the query of the current field.
        /// </summary>
        public int FieldDepth { get; }

        /// <summary>
        /// Gets the selection depth of the syntax tree.
        /// </summary>
        public int NodeDepth { get; }

        /// <summary>
        /// A helper to resolver a multiplier argument value.
        /// </summary>
        public bool TryGetArgumentValue<T>(string name, [NotNullWhen(true)] out T value)
        {
            if (Field.Arguments.TryGetField(name, out IInputField? argument))
            {
                IValueNode? argumentValue = Selection.Arguments
                    .FirstOrDefault(t => StringExtensions.EqualsOrdinal(t.Name.Value, name))?
                    .Value;

                if (argumentValue is VariableNode variable &&
                    _valueCollection.TryGetVariable(variable.Name.Value, out T castedVariable))
                {
                    value = castedVariable;
                    return true;
                }

                if (argumentValue is not null)
                {
                    try
                    {
                        if (argument.Type.ParseLiteral(argumentValue) is T castedArgument)
                        {
                            value = castedArgument;
                            return true;
                        }
                    }
                    catch (SerializationException)
                    {
                        // we ignore serialization errors and fall through.
                    }
                }
            }

            value = default!;
            return false;
        }
    }
}
