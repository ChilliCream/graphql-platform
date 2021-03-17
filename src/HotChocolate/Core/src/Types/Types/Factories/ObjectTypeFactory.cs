using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(
            IBindingLookup bindingLookup,
            IReadOnlySchemaOptions schemaOptions,
            ObjectTypeDefinitionNode node)
        {
            if (bindingLookup is null)
            {
                throw new ArgumentNullException(nameof(bindingLookup));
            }

            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ITypeBindingInfo bindingInfo =
                bindingLookup.GetBindingInfo(node.Name.Value);

            return new ObjectType(d =>
            {
                d.SyntaxNode(schemaOptions.PreserveSyntaxNodes ? node : null)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(t => t.RuntimeType = bindingInfo.SourceType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareInterfaces(d,schemaOptions, node.Interfaces);

                DeclareFields(bindingInfo,schemaOptions, d, node.Fields);
            });
        }

        private static void DeclareInterfaces(
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlySchemaOptions schemaOptions,
            IReadOnlyCollection<NamedTypeNode> interfaceReferences)
        {
            foreach (NamedTypeNode typeNode in interfaceReferences)
            {
                typeDescriptor.Interface(typeNode);
            }
        }

        private static void DeclareFields(
            ITypeBindingInfo bindingInfo,
            IReadOnlySchemaOptions schemaOptions,
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlyCollection<FieldDefinitionNode> fieldDefinitions)
        {
            foreach (FieldDefinitionNode fieldDefinition in fieldDefinitions)
            {
                bindingInfo.TrackField(fieldDefinition.Name.Value);

                IObjectFieldDescriptor fieldDescriptor = typeDescriptor
                    .Field(fieldDefinition.Name.Value)
                    .Description(fieldDefinition.Description?.Value)
                    .Type(fieldDefinition.Type)
                    .SyntaxNode(schemaOptions.PreserveSyntaxNodes ? fieldDefinition : null);

                if (bindingInfo.TryGetFieldMember(
                    fieldDefinition.Name.Value,
                    MemberKind.ObjectField,
                    out MemberInfo member))
                {
                    fieldDescriptor.Extend().OnBeforeCreate(
                        t => t.Member = member);
                }

                foreach (DirectiveNode directive in fieldDefinition.Directives)
                {
                    if (!directive.IsDeprecationReason())
                    {
                        fieldDescriptor.Directive(directive);
                    }
                }

                string deprecationReason = fieldDefinition.DeprecationReason();
                if (!string.IsNullOrEmpty(deprecationReason))
                {
                    fieldDescriptor.Deprecated(deprecationReason);
                }

                DeclareFieldArguments(schemaOptions, fieldDescriptor, fieldDefinition);
            }
        }

        private static void DeclareFieldArguments(
            IReadOnlySchemaOptions schemaOptions,
            IObjectFieldDescriptor fieldDescriptor,
            FieldDefinitionNode fieldDefinition)
        {
            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                fieldDescriptor.Argument(inputFieldDefinition.Name.Value,
                    a =>
                    {
                        IArgumentDescriptor descriptor = a
                            .Description(inputFieldDefinition.Description?.Value)
                            .Type(inputFieldDefinition.Type)
                            .DefaultValue(inputFieldDefinition.DefaultValue)
                            .SyntaxNode(schemaOptions.PreserveSyntaxNodes
                                ? inputFieldDefinition
                                : null);

                        foreach (DirectiveNode directive in
                            inputFieldDefinition.Directives)
                        {
                            descriptor.Directive(directive);
                        }
                    });
            }
        }
    }
}
