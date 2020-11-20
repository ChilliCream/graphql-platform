using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal static class TypeDependencyHelper
    {
        public static void RegisterDependencies(
            this ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            context.RegisterDependencyRange(
                definition.Interfaces,
                TypeDependencyKind.Completed);

            RegisterAdditionalDependencies(context, definition);
            RegisterDirectiveDependencies(context, definition);
            RegisterFieldDependencies(context, definition.Fields);

            foreach (ObjectFieldDefinition field in definition.Fields)
            {
                if (field.Resolver is null)
                {
                    if (field.Expression is not null)
                    {
                        context.RegisterResolver(
                            field.Name,
                            field.Expression,
                            definition.RuntimeType,
                            field.ResolverType);
                    }
                    else if (field.ResolverMember is not null)
                    {
                        context.RegisterResolver(
                            field.Name,
                            field.ResolverMember,
                            definition.RuntimeType,
                            field.ResolverType);
                    }
                    else if (field.Member is not null)
                    {
                        context.RegisterResolver(
                            field.Name,
                            field.Member,
                            definition.RuntimeType,
                            field.ResolverType);
                    }
                }
            }
        }

        public static void RegisterDependencies(
            this ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            context.RegisterDependencyRange(
                definition.Interfaces,
                TypeDependencyKind.Completed);

            RegisterAdditionalDependencies(context, definition);
            RegisterDirectiveDependencies(context, definition);
            RegisterFieldDependencies(context, definition.Fields);
        }

        public static void RegisterDependencies(
            this ITypeDiscoveryContext context,
            EnumTypeDefinition definition)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            RegisterAdditionalDependencies(context, definition);
            RegisterDirectiveDependencies(context, definition);
            RegisterEnumValueDependencies(context, definition.Values);
        }

        public static void RegisterDependencies(
            this ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            RegisterAdditionalDependencies(context, definition);
            RegisterDirectiveDependencies(context, definition);

            foreach (InputFieldDefinition field in definition.Fields)
            {
                RegisterAdditionalDependencies(context, field);

                if (field.Type is not null)
                {
                    context.RegisterDependency(field.Type,
                        TypeDependencyKind.Default);
                }

                context.RegisterDependencyRange(
                    field.Directives.Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }
        }

        private static void RegisterDirectiveDependencies<T>(
            this ITypeDiscoveryContext context,
            TypeDefinitionBase<T> definition)
            where T : class, ISyntaxNode
        {
            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
        }

        private static void RegisterAdditionalDependencies(
            this ITypeDiscoveryContext context,
            DefinitionBase definition)
        {
            context.RegisterDependencyRange(
                definition.Dependencies);
        }

        private static void RegisterFieldDependencies(
            this ITypeDiscoveryContext context,
            IReadOnlyList<OutputFieldDefinitionBase> fields)
        {
            foreach (OutputFieldDefinitionBase field in fields)
            {
                RegisterAdditionalDependencies(context, field);

                if (field.Type is not null)
                {
                    context.RegisterDependency(field.Type,
                        TypeDependencyKind.Default);
                }

                context.RegisterDependencyRange(
                    field.Directives.Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }

            RegisterFieldDependencies(context,
                fields.SelectMany(t => t.Arguments));
        }

        private static void RegisterFieldDependencies(
            this ITypeDiscoveryContext context,
            IEnumerable<ArgumentDefinition> fields)
        {
            foreach (ArgumentDefinition field in fields)
            {
                RegisterAdditionalDependencies(context, field);

                if (field.Type is not null)
                {
                    context.RegisterDependency(field.Type,
                        TypeDependencyKind.Completed);
                }

                context.RegisterDependencyRange(
                    field.Directives.Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }
        }

        private static void RegisterEnumValueDependencies(
            this ITypeDiscoveryContext context,
            IEnumerable<EnumValueDefinition> enumValues)
        {
            foreach (EnumValueDefinition enumValue in enumValues)
            {
                RegisterAdditionalDependencies(context, enumValue);

                context.RegisterDependencyRange(
                    enumValue.Directives.Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }
        }
    }
}
