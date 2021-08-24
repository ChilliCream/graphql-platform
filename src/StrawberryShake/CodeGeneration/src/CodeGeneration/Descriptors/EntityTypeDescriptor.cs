using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    public class EntityTypeDescriptor : ICodeDescriptor
    {
        /// <summary>
        /// Create a new instance of <see cref="EntityTypeDescriptor" />
        /// </summary>
        /// <param name="name">
        /// The name of the GraphQL type
        /// </param>
        /// <param name="runtimeType"></param>
        /// <param name="possibleTypes">
        /// The possible types this entity can have
        /// </param>
        /// <param name="documentation">
        /// The documentation of this entity
        /// </param>
        public EntityTypeDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType,
            IReadOnlyList<ComplexTypeDescriptor> possibleTypes,
            string? documentation)
        {
            var allProperties = new Dictionary<string, PropertyDescriptor>();

            foreach (PropertyDescriptor namedTypeReferenceDescriptor in
                possibleTypes.SelectMany(operationType => operationType.Properties))
            {
                if (!allProperties.ContainsKey(namedTypeReferenceDescriptor.Name))
                {
                    allProperties
                        .Add(namedTypeReferenceDescriptor.Name, namedTypeReferenceDescriptor);
                }
            }

            Name = name;
            RuntimeType = runtimeType;
            Properties = allProperties;
            Documentation = documentation;
        }

        /// <summary>
        /// Gets the GraphQL type name which this entity represents.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the entity name.
        /// </summary>
        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// The documentation of this type
        /// </summary>
        public string? Documentation { get; }

        /// <summary>
        /// Gets the properties of this entity.
        /// </summary>
        public Dictionary<string, PropertyDescriptor> Properties { get; }
    }
}
