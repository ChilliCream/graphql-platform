using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Internal
{
    public static class TypeDependencyHelper
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
                definition.GetInterfaces(),
                TypeDependencyKind.Completed);

            RegisterAdditionalDependencies(context, definition);
            RegisterDirectiveDependencies(context, definition);
            RegisterFieldDependencies(context, definition.Fields);
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
                definition.GetInterfaces(),
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
                    field.GetDirectives().Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }
        }

        private static void RegisterDirectiveDependencies<T>(
            this ITypeDiscoveryContext context,
            TypeDefinitionBase<T> definition)
            where T : class, ISyntaxNode
        {
            context.RegisterDependencyRange(
                definition.GetDirectives().Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
        }

        private static void RegisterAdditionalDependencies(
            this ITypeDiscoveryContext context,
            DefinitionBase definition)
        {
            context.RegisterDependencyRange(definition.GetDependencies());
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
                    field.GetDirectives().Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }

            RegisterFieldDependencies(context,
                fields.SelectMany(t => t.GetArguments()));
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
                    field.GetDirectives().Select(t => t.TypeReference),
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
                    enumValue.GetDirectives().Select(t => t.TypeReference),
                    TypeDependencyKind.Completed);
            }
        }
    }
}
