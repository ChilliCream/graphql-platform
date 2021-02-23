using System.Collections.Generic;
using System.Linq;
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
            IReadOnlyList<ITypeDescriptor> typeDescriptors)
        {
            RuntimeType = new(name, @namespace);
            Entities = entities;
            Operations = operations;
            TypeDescriptors = typeDescriptors;
            EnumTypeDescriptor = typeDescriptors.OfType<EnumTypeDescriptor>().ToList();;
        }

        /// <summary>
        /// The name of the client
        /// </summary>
        public NameString Name => RuntimeType.Name;

        public RuntimeTypeInfo RuntimeType { get; }

        public IReadOnlyList<EntityTypeDescriptor> Entities { get; }

        public IReadOnlyList<ITypeDescriptor> TypeDescriptors { get; }

        public IReadOnlyList<EnumTypeDescriptor> EnumTypeDescriptor { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }
    }
}
