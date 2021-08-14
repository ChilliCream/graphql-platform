using System;
using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public class PrimaryKeyField
    {
        public NameString Name { get; }
        public Type RuntimeType { get; }

        public PrimaryKeyField(NameString name, Type runtimeType)
        {
            Name = name;
            RuntimeType = runtimeType ?? throw new ArgumentNullException(nameof(runtimeType));
        }

        public PrimaryKeyField(IObjectField objectField)
            : this(objectField.Name, objectField.RuntimeType)
        {

        }
    }
}
