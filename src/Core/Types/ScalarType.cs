using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class ScalarType
        : INamedType
        , IOutputType
        , IInputType
        , INullableType
        , ISerializableType
    // TODO : ITypeSystemNode
    {
        protected ScalarType(string name)
            : this(name, null)
        {
        }

        protected ScalarType(string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Description = description;
        }

        public TypeKind Kind { get; } = TypeKind.Scalar;

        public string Name { get; }

        public virtual string Description { get; }

        public abstract Type NativeType { get; }

        public abstract bool IsInstanceOfType(IValueNode literal);

        public abstract object ParseLiteral(IValueNode literal);

        public abstract IValueNode ParseValue(object value);

        public abstract object Serialize(object value);
    }
}
