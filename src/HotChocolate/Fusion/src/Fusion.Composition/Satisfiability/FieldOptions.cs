using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The options of an output field: its event stream schemas when it has any, otherwise its
/// schemas, in the order the field declares them.
/// </summary>
internal sealed class FieldOptions
{
    private readonly FieldOption[] _options;

    /// <summary>
    /// Creates the options <paramref name="options"/> of <paramref name="field"/>.
    /// </summary>
    public FieldOptions(MutableOutputFieldDefinition field, FieldOption[] options)
    {
        Field = field;
        FieldType = field.Type.AsTypeDefinition();
        ReturnsCompositeType = FieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition;
        ReturnsLeafType = FieldType.IsLeafType();
        _options = options;
    }

    /// <summary>
    /// Gets the field.
    /// </summary>
    public MutableOutputFieldDefinition Field { get; }

    /// <summary>
    /// Gets the named type the field returns.
    /// </summary>
    public ITypeDefinition FieldType { get; }

    /// <summary>
    /// Gets a value indicating whether the field returns an object, interface or union type.
    /// </summary>
    public bool ReturnsCompositeType { get; }

    /// <summary>
    /// Gets a value indicating whether the field returns a scalar or enum type.
    /// </summary>
    public bool ReturnsLeafType { get; }

    /// <summary>
    /// Enumerates the options in declaration order, with the first option in
    /// <paramref name="schemaName"/> moved to the front and the other options in that schema left
    /// out. When <paramref name="schemaName"/> is <c>null</c> or has no option, every option is
    /// enumerated in declaration order.
    /// </summary>
    public Enumerator StartingWith(string? schemaName)
    {
        var first = -1;

        if (schemaName is not null)
        {
            for (var i = 0; i < _options.Length; i++)
            {
                if (string.Equals(_options[i].SchemaName, schemaName, StringComparison.Ordinal))
                {
                    first = i;
                    break;
                }
            }
        }

        return new Enumerator(_options, first);
    }

    /// <summary>
    /// Enumerates the options of a field in the order of <see cref="StartingWith"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly FieldOption[] _options;
        private readonly int _first;
        private int _position;

        internal Enumerator(FieldOption[] options, int first)
        {
            _options = options;
            _first = first;
            _position = first < 0 ? -1 : -2;
            Current = null!;
        }

        /// <summary>
        /// Gets the current option.
        /// </summary>
        public FieldOption Current { get; private set; }

        /// <summary>
        /// Advances to the next option.
        /// </summary>
        public bool MoveNext()
        {
            if (_position == -2)
            {
                _position = -1;
                Current = _options[_first];
                return true;
            }

            while (++_position < _options.Length)
            {
                var option = _options[_position];

                if (_first < 0 || option.SchemaIndex != _options[_first].SchemaIndex)
                {
                    Current = option;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets this enumerator.
        /// </summary>
        public readonly Enumerator GetEnumerator() => this;
    }
}
