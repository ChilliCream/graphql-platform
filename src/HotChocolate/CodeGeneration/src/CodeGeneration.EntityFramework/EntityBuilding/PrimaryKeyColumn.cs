using System;
using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public class PrimaryKeyColumn
    {
        public Type RuntimeType { get; }

        /// <summary>
        /// The field on the object type.
        /// Null if there's none and this PK is auto-generated.
        /// </summary>
        public IObjectField? Field { get; }

        public PrimaryKeyColumn(Type runtimeType)
        {
            RuntimeType = runtimeType ?? throw new ArgumentNullException(nameof(runtimeType));
        }

        public PrimaryKeyColumn(IObjectField objectField)
            : this(objectField.RuntimeType)
        {
            Field = objectField;
        }
    }
}
