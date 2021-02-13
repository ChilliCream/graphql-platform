using System.Collections.Generic;
using System.Linq;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EntityTypeDescriptor : ICodeDescriptor
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="graphQLTypeName">
        ///
        /// </param>
        /// <param name="namespace">
        ///
        /// </param>
        /// <param name="operationTypes">
        /// The types that are subsets of the EntityType represented by this descriptor.
        /// </param>
        public EntityTypeDescriptor(
            NameString graphQLTypeName,
            string @namespace,
            IReadOnlyList<NamedTypeDescriptor> operationTypes)
        {
            var allProperties = new Dictionary<string, PropertyDescriptor>();

            foreach (PropertyDescriptor namedTypeReferenceDescriptor in
                operationTypes.SelectMany(operationType => operationType.Properties))
            {
                if (!allProperties.ContainsKey(namedTypeReferenceDescriptor.Name))
                {
                    allProperties.Add(
                        namedTypeReferenceDescriptor.Name,
                        namedTypeReferenceDescriptor);
                }
            }

            Properties = allProperties;
            Name = NamingConventions.EntityTypeNameFromGraphQLTypeName(graphQLTypeName);
            GraphQLTypeName = graphQLTypeName;
            Namespace = @namespace;
        }

        /// <summary>
        /// Gets the entity name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the GraphQL type name which this entity represents.
        /// </summary>
        public NameString GraphQLTypeName { get; }

        /// <summary>
        /// Gets the namespace of the generated code file.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the properties of this entity.
        /// </summary>
        public Dictionary<string, PropertyDescriptor> Properties { get; }
    }
}
