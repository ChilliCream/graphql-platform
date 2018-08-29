using System;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public class FieldResolverArgumentDescriptor
    {
        internal FieldResolverArgumentDescriptor(
            string name, string variableName,
            FieldResolverArgumentKind kind,
            Type type)
        {
            Name = name;
            VariableName = variableName;
            Kind = kind;
            Type = type;
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the variable name of the argument.
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// Defines the argument kind.
        /// </summary>
        /// <returns></returns>
        public FieldResolverArgumentKind Kind { get; }

        /// <summary>
        /// Gets the argument type.
        /// </summary>
        public Type Type { get; }
    }
}
