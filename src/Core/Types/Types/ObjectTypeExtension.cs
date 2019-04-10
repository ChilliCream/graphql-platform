using System.Collections.Generic;
using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;

namespace HotChocolate.Types
{
    public class ObjectTypeExtension
        : NamedTypeExtensionBase<ObjectTypeDefinition>
    {
        private readonly Action<IObjectTypeDescriptor> _configure;

        protected ObjectTypeExtension()
        {
            _configure = Configure;
        }

        public ObjectTypeExtension(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Object;

        protected override ObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        internal override void Merge(
            ICompletionContext context,
            INamedType type)
        {
            if (type is ObjectType objectType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    objectType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    objectType.Definition.Directives);

                TypeExtensionHelper.MergeObjectFields(
                    context,
                    Definition.Fields,
                    objectType.Definition.Fields);
            }

            // TODO : resources
            throw new ArgumentException("CANNOT MERGE");
        }
    }

    internal static class TypeExtensionHelper
    {
        public static void MergeObjectFields(
            ICompletionContext context,
            IList<ObjectFieldDefinition> extensionFields,
            IList<ObjectFieldDefinition> typeFields)
        {
            MergeOutputFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (extensionField.Resolver != null)
                    {
                        typeField.Resolver = extensionField.Resolver;
                    }
                });
        }

        public static void MergeInterfaceFields(
            ICompletionContext context,
            IList<InterfaceFieldDefinition> extensionFields,
            IList<InterfaceFieldDefinition> typeFields)
        {
            MergeOutputFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) => { });
        }

        private static void MergeOutputFields<T>(
            ICompletionContext context,
            IList<T> extensionFields,
            IList<T> typeFields,
            Action<IList<T>, T, T> action)
            where T : OutputFieldDefinitionBase
        {
            MergeFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (extensionField.IsDeprecated)
                    {
                        typeField.DeprecationReason =
                            extensionField.DeprecationReason;
                    }

                    MergeFields(
                        context,
                        extensionField.Arguments,
                        typeField.Arguments,
                        (args, extensionArg, typeArg) => { });

                    action(fields, extensionField, typeField);
                });
        }

        private static void MergeFields<T>(
            ICompletionContext context,
            IList<T> extensionFields,
            IList<T> typeFields,
            Action<IList<T>, T, T> action)
            where T : FieldDefinitionBase
        {
            foreach (T extensionField in extensionFields)
            {
                T typeField = typeFields
                    .FirstOrDefault(t => t.Name.Equals(extensionField.Name));

                if (typeField == null)
                {
                    typeFields.Add(extensionField);
                }
                else
                {
                    MergeDirectives(
                        context,
                        extensionField.Directives,
                        typeField.Directives);

                    MergeContextData(extensionField, typeField);

                    action(typeFields, extensionField, typeField);
                }
            }
        }

        public static void MergeDirectives(
            ICompletionContext context,
            IList<DirectiveDefinition> extension,
            IList<DirectiveDefinition> type)
        {
            var directives =
                new List<(DirectiveType type, DirectiveDefinition def)>();

            foreach (DirectiveDefinition directive in extension)
            {
                DirectiveType directiveType =
                    context.GetDirectiveType(directive.Reference);
                directives.Add((directiveType, directive));
            }

            foreach (DirectiveDefinition directive in extension)
            {
                MergeDirective(context, directives, directive);
            }
        }

        private static void MergeDirective(
            ICompletionContext context,
            IList<(DirectiveType type, DirectiveDefinition def)> directives,
            DirectiveDefinition directive)
        {
            DirectiveType directiveType =
                context.GetDirectiveType(directive.Reference);

            if (directiveType != null)
            {
                if (directiveType.IsRepeatable)
                {
                    directives.Add((directiveType, directive));
                }
                else
                {
                    var entry = directives
                        .FirstOrDefault(t => t.type == directiveType);
                    if (entry == default)
                    {
                        directives.Add((directiveType, directive));
                    }
                    else
                    {
                        int index = directives.IndexOf(entry);
                        directives[index] = (directiveType, directive);
                    }
                }
            }
        }

        public static void MergeContextData(
            DefinitionBase extension,
            DefinitionBase type)
        {
            if (extension.ContextData.Count > 0)
            {
                foreach (KeyValuePair<string, object> entry in
                    extension.ContextData)
                {
                    type.ContextData[entry.Key] = entry.Value;
                }
            }
        }
    }
}
