using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class RegisteredType
        : IHasClrType
    {
        public RegisteredType(TypeSystemObjectBase type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public TypeSystemObjectBase Type { get; }

        public Type ClrType
        {
            get
            {
                if (Type is IHasClrType hasClrType)
                {
                    return hasClrType.ClrType;
                }
                return typeof(object);
            }
        }

        public ICollection<TypeDependency> Dependencies { get; } =
            new List<TypeDependency>();
    }
}
