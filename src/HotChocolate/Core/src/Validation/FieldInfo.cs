using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public readonly struct FieldInfo
    {
        public FieldInfo(IType declaringType, IType type, FieldNode field)
        {
            DeclaringType = declaringType;
            Type = type;
            Field = field;
            ResponseName = Field.Alias is null
                ? Field.Name.Value
                : Field.Alias.Value;
        }

        /// <summary>
        /// Gets the response name.
        /// </summary>
        public string ResponseName { get; }

        /// <summary>
        /// Gets the declaring type.
        /// </summary>
        public IType DeclaringType { get; }

        /// <summary>
        /// Gets the field's return type.
        /// </summary>
        public IType Type { get; }

        /// <summary>
        /// Gets the field selection.
        /// </summary>
        public FieldNode Field { get; }
    }
}
