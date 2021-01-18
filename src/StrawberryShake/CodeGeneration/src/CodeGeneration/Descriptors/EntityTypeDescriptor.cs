using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
            string graphQLTypeName,
            string @namespace,
            IReadOnlyList<TypeDescriptor> operationTypes)
        {
            var allProperties = new Dictionary<string, NamedTypeReferenceDescriptor>();

            foreach (NamedTypeReferenceDescriptor namedTypeReferenceDescriptor in
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
            GraphQLTypeName = graphQLTypeName;
            Namespace = @namespace;
        }

        public string GraphQLTypeName { get; }

        public string Namespace { get; }

        public Dictionary<string, NamedTypeReferenceDescriptor> Properties { get; }
    }
}
