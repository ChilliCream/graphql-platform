using System;

namespace HotChocolate.Resolvers
{
    public class FieldResolverArgumentDescriptor
    {
        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name { get; }

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