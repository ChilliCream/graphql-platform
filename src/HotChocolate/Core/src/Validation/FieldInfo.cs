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
            ResponseName = Field.Alias == null
                ? Field.Name.Value
                : Field.Alias.Value;
        }

        public string ResponseName { get; }
        public IType DeclaringType { get; }
        public IType Type { get; }
        public FieldNode Field { get; }
    }
}
