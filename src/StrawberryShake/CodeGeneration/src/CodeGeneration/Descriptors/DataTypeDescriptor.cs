using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class DataTypeDescriptor: ICodeDescriptor
    {
        /// <summary>
        /// Describes the DataType
        /// </summary>
        /// <param name="graphQLTypeName">
        ///
        /// </param>
        /// <param name="namespace">
        ///
        /// </param>
        /// <param name="operationTypes">
        /// The types that are subsets of the DataType represented by this descriptor.
        /// </param>
        public DataTypeDescriptor(
            NameString graphQLTypeName,
            string @namespace,
            IReadOnlyList<NamedTypeDescriptor> operationTypes,
            IReadOnlyList<string> implements,
            bool isInterface = false)
        {
            var allProperties = new Dictionary<string, PropertyDescriptor>();

            foreach (PropertyDescriptor namedTypeReferenceDescriptor in
                operationTypes.SelectMany(operationType => operationType.Properties))
            {
                if (!allProperties.ContainsKey(namedTypeReferenceDescriptor.Name))
                {
                    if (namedTypeReferenceDescriptor.Type is NonNullTypeDescriptor nonNull)
                    {
                        allProperties.Add(
                            namedTypeReferenceDescriptor.Name,
                            new PropertyDescriptor(
                                namedTypeReferenceDescriptor.Name,
                                nonNull.InnerType)
                            );
                    }
                    else
                    {
                        allProperties.Add(
                            namedTypeReferenceDescriptor.Name,
                            namedTypeReferenceDescriptor);
                    }
                }
            }

            Properties = allProperties.Select(pair => pair.Value).ToList();
            Name = NamingConventions.DataTypeNameFromTypeName(graphQLTypeName);
            GraphQLTypeName = graphQLTypeName;
            Namespace = @namespace;
            Implements = implements;
            IsInterface = isInterface;
        }

        public bool IsInterface { get; set; }

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
        public IReadOnlyList<PropertyDescriptor> Properties { get; }

        /// <summary>
        /// The interfaces that this data type implements. A data type does only implement
        /// interfaces, if it is part of a graphql union or interface type.
        /// </summary>
        public IReadOnlyList<string> Implements { get; }
    }
}
