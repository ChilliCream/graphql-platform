using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StrawberryShake.CodeGeneration
{
    public class EntityTypeDescriptor: ICodeDescriptor
    {
        public string GraphQLTypename { get; }
        public string Namespace { get; }
        public Dictionary<string, NamedTypeReferenceDescriptor> Properties { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="operationTypes">The types that are subsets of the EntityType represented by this descriptor</param>
        /// <param name="graphQLTypename"></param>
        public EntityTypeDescriptor(
            string graphQLTypename,
            string @nameSpace,
            IReadOnlyList<TypeDescriptor> operationTypes
        )
        {
            var allProperties = new Dictionary<string, NamedTypeReferenceDescriptor>();
            foreach (NamedTypeReferenceDescriptor namedTypeReferenceDescriptor in operationTypes.SelectMany(operationType => operationType.Properties))
            {
                if (!allProperties.ContainsKey(namedTypeReferenceDescriptor.Name))
                {
                    allProperties.Add(namedTypeReferenceDescriptor.Name, namedTypeReferenceDescriptor);
                }
            }

            Properties = allProperties;
            GraphQLTypename = graphQLTypename;
            Namespace = @nameSpace;
        }
    }
}
