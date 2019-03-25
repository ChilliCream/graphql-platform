using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types.Factories
{
    internal sealed class DirectiveTypeFactory
        : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
    {
        private static readonly Dictionary<Language.DirectiveLocation, DirectiveLocation> _locations =
            new Dictionary<Language.DirectiveLocation, DirectiveLocation>
            {
                { Language.DirectiveLocation.Query, DirectiveLocation.Query },
                { Language.DirectiveLocation.Mutation, DirectiveLocation.Mutation },
                { Language.DirectiveLocation.Subscription, DirectiveLocation.Subscription },
                { Language.DirectiveLocation.Field, DirectiveLocation.Field },
                { Language.DirectiveLocation.FragmentDefinition, DirectiveLocation.FragmentDefinition },
                { Language.DirectiveLocation.FragmentSpread, DirectiveLocation.FragmentSpread },
                { Language.DirectiveLocation.InlineFragment, DirectiveLocation.InlineFragment },
                { Language.DirectiveLocation.Schema, DirectiveLocation.Schema },
                { Language.DirectiveLocation.Scalar, DirectiveLocation.Scalar },
                { Language.DirectiveLocation.Object, DirectiveLocation.Object },
                { Language.DirectiveLocation.FieldDefinition, DirectiveLocation.FieldDefinition },
                { Language.DirectiveLocation.ArgumentDefinition, DirectiveLocation.ArgumentDefinition },
                { Language.DirectiveLocation.Interface, DirectiveLocation.Interface },
                { Language.DirectiveLocation.Union, DirectiveLocation.Union },
                { Language.DirectiveLocation.Enum, DirectiveLocation.Enum },
                { Language.DirectiveLocation.EnumValue, DirectiveLocation.EnumValue },
                { Language.DirectiveLocation.InputObject, DirectiveLocation.InputObject },
                { Language.DirectiveLocation.InputFieldDefinition, DirectiveLocation.InputFieldDefinition },
            };

        public DirectiveType Create(DirectiveDefinitionNode node)
        {
            return new DirectiveType(c =>
            {
                c.Name(node.Name.Value);
                c.Description(node.Description?.Value);
                c.SyntaxNode(node);

                if (node.IsRepeatable)
                {
                    c.Repeatable();
                }

                DeclareArguments(c, node);
                DeclareLocations(c, node);
            });
        }

        private static void DeclareArguments(
            IDirectiveTypeDescriptor typeDescriptor,
            DirectiveDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Arguments)
            {
                IArgumentDescriptor descriptor = typeDescriptor
                    .Argument(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .DefaultValue(inputField.DefaultValue)
                    .SyntaxNode(inputField);

                foreach (DirectiveNode directive in inputField.Directives)
                {
                    descriptor.Directive(directive);
                }
            }
        }

        private static void DeclareLocations(
            IDirectiveTypeDescriptor typeDescriptor,
            DirectiveDefinitionNode node)
        {
            foreach (NameNode location in node.Locations)
            {
                if (Language.DirectiveLocation.TryParse(
                    location.Value,
                    out Language.DirectiveLocation parsedLocation))
                {
                    typeDescriptor.Location(MapDirectiveLocation(parsedLocation));
                }
            }
        }

        private static DirectiveLocation MapDirectiveLocation(
            Language.DirectiveLocation location)
        {
            if (!_locations.TryGetValue(location, out DirectiveLocation l))
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveTypeFactory_LocationNotSupported,
                    location));
            }
            return l;
        }
    }
}
