using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StrawberryShake.CodeGeneration
{
    public class EntityTypeDescriptor: ICodeDescriptor
    {
        public string GraphQlTypename { get; }
        public string Namespace { get; }
        public Dictionary<string, NamedTypeReferenceDescriptor> Properties { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="operationTypes">The types that are subsets of the EntityType represented by this descriptor</param>
        /// <param name="graphQlTypename"></param>
        public EntityTypeDescriptor(
            string graphQlTypename,
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
            GraphQlTypename = graphQlTypename;
            Namespace = @nameSpace;
        }
    }
}
