using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class InterfaceTypeDescriptor : ComplexTypeDescriptor
    {
        public InterfaceTypeDescriptor(
            NameString name,
            TypeKind typeKind,
            RuntimeTypeInfo runtimeType,
            IReadOnlyList<ObjectTypeDescriptor> implementedBy,
            IReadOnlyList<NameString> implements,
            RuntimeTypeInfo? parentRuntimeType = null)
            : base(name, typeKind, runtimeType, implements, parentRuntimeType)
        {
            ImplementedBy = implementedBy;
        }

        /// <summary>
        /// A list of types that implement this interface
        /// This list must only contain the most specific, concrete classes (that implement this 
        /// interface), but no other interfaces.
        /// </summary>
        public IReadOnlyList<ObjectTypeDescriptor> ImplementedBy { get; }
    }
}
