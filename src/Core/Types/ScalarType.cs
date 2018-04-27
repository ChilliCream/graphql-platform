using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class ScalarType
        : IOutputType
        , IInputType
        , INamedType
        , INullableType
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

        public string Name { get; }

        public virtual string Description { get; }

        public abstract bool IsInstanceOfType(IValueNode literal);

        public abstract object ParseLiteral(IValueNode literal, Type targetType);

        public abstract string Serialize(object value);
    }
}
