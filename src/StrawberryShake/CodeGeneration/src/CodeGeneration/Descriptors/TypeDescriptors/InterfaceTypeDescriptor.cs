using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public class InterfaceTypeDescriptor : ComplexTypeDescriptor
    {
        public InterfaceTypeDescriptor(
            NameString name,
            TypeKind typeKind,
            RuntimeTypeInfo runtimeType,
            IReadOnlyCollection<ObjectTypeDescriptor> implementedBy,
            IReadOnlyList<NameString> implements,
            string? description,
            RuntimeTypeInfo? parentRuntimeType = null)
            : base(name, typeKind, runtimeType, implements, description, parentRuntimeType)
        {
            ImplementedBy = implementedBy;
        }

        /// <summary>
        /// A list of types that implement this interface
        /// This list must only contain the most specific, concrete classes (that implement this
        /// interface), but no other interfaces.
        /// </summary>
        public IReadOnlyCollection<ObjectTypeDescriptor> ImplementedBy { get; }
    }
}
