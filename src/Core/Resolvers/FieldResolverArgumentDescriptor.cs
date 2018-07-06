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
            FieldResolverArgumentKind kind, Type type)
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

        internal static FieldResolverArgumentKind LookupKind(
            Type argumentType, Type sourceType)
        {
            if (argumentType == sourceType)
            {
                return FieldResolverArgumentKind.Source;
            }

            if (argumentType == typeof(IResolverContext))
            {
                return FieldResolverArgumentKind.Context;
            }

            if (argumentType == typeof(Schema))
            {
                return FieldResolverArgumentKind.Schema;
            }

            if (argumentType == typeof(ObjectType))
            {
                return FieldResolverArgumentKind.ObjectType;
            }

            if (argumentType == typeof(ObjectField))
            {
                return FieldResolverArgumentKind.Field;
            }

            if (argumentType == typeof(CancellationToken))
            {
                return FieldResolverArgumentKind.CancellationToken;
            }

            if (argumentType == typeof(DocumentNode))
            {
                return FieldResolverArgumentKind.QueryDocument;
            }

            if (argumentType == typeof(OperationDefinitionNode))
            {
                return FieldResolverArgumentKind.OperationDefinition;
            }

            if (argumentType == typeof(FieldNode))
            {
                return FieldResolverArgumentKind.Field;
            }

            // Service -> Attribute

            return FieldResolverArgumentKind.Argument;
        }
    }
}
