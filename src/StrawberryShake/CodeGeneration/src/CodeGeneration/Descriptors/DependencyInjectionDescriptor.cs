using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes the dependency injection requirements of a  GraphQL client
    /// </summary>
    public class DependencyInjectionDescriptor : ICodeDescriptor
    {
        public DependencyInjectionDescriptor(
            NameString name,
            string @namespace,
            IReadOnlyList<EntityTypeDescriptor> entities,
            List<OperationDescriptor> operations,
            IReadOnlyList<ITypeDescriptor> typeDescriptors,
            IReadOnlyList<EnumTypeDescriptor> enumTypeDescriptor)
        {
            Name = name;
            Namespace = @namespace;
            Entities = entities;
            Operations = operations;
            TypeDescriptors = typeDescriptors;
            EnumTypeDescriptor = enumTypeDescriptor;
        }

        /// <summary>
        /// The name of the client
        /// </summary>
        public NameString Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<EntityTypeDescriptor> Entities { get; }

        public IReadOnlyList<ITypeDescriptor> TypeDescriptors { get; }

        public IReadOnlyList<EnumTypeDescriptor> EnumTypeDescriptor { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }
    }
}
