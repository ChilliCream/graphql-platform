using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public ResultTypeDescriptor(
            string name,
            string graphQLTypeName,
            IReadOnlyList<ResultTypeComponentDescriptor> components,
            IReadOnlyList<ResultFieldDescriptor> fields)
        {
            Name = name;
            Components = components;
            Fields = fields;
        }

        /// <summary>
        /// Gets the .NET type name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the GraphQL type name of the inner named type.
        /// </summary>
        public string GraphQLTypeName { get; }

        public IReadOnlyList<ResultTypeComponentDescriptor> Components { get; }

        public IReadOnlyList<ResultFieldDescriptor> Fields { get; }
    }
}
