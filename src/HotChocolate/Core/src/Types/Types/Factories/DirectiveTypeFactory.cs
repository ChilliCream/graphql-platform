using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class DirectiveTypeFactory
        : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
    {
        private static readonly Dictionary<Language.DirectiveLocation, DirectiveLocation> _locs =
            new()
            {
                    {
                        Language.DirectiveLocation.Query,
                        DirectiveLocation.Query
                    },
                    {
                        Language.DirectiveLocation.Mutation,
                        DirectiveLocation.Mutation
                    },
                    {
                        Language.DirectiveLocation.Subscription,
                        DirectiveLocation.Subscription
                    },
                    {
                        Language.DirectiveLocation.Field,
                        DirectiveLocation.Field
                    },
                    {
                        Language.DirectiveLocation.FragmentDefinition,
                        DirectiveLocation.FragmentDefinition
                    },
                    {
                        Language.DirectiveLocation.FragmentSpread,
                        DirectiveLocation.FragmentSpread
                    },
                    {
                        Language.DirectiveLocation.InlineFragment,
                        DirectiveLocation.InlineFragment
                    },
                    {
                        Language.DirectiveLocation.Schema,
                        DirectiveLocation.Schema
                    },
                    {
                        Language.DirectiveLocation.Scalar,
                        DirectiveLocation.Scalar
                    },
                    {
                        Language.DirectiveLocation.Object,
                        DirectiveLocation.Object
                    },
                    {
                        Language.DirectiveLocation.FieldDefinition,
                        DirectiveLocation.FieldDefinition
                    },
                    {
                        Language.DirectiveLocation.ArgumentDefinition,
                        DirectiveLocation.ArgumentDefinition
                    },
                    {
                        Language.DirectiveLocation.Interface,
                        DirectiveLocation.Interface
                    },
                    {
                        Language.DirectiveLocation.Union,
                        DirectiveLocation.Union
                    },
                    {
                        Language.DirectiveLocation.Enum,
                        DirectiveLocation.Enum
                    },
                    {
                        Language.DirectiveLocation.EnumValue,
                        DirectiveLocation.EnumValue
                    },
                    {
                        Language.DirectiveLocation.InputObject,
                        DirectiveLocation.InputObject
                    },
                    {
                        Language.DirectiveLocation.InputFieldDefinition,
                        DirectiveLocation.InputFieldDefinition
                    },
            };

        public DirectiveType Create(IDescriptorContext context, DirectiveDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new DirectiveTypeDefinition(
                node.Name.Value,
                node.Description?.Value,
                isRepeatable: node.IsRepeatable);

            if (context.Options.DefaultDirectiveVisibility is DirectiveVisibility.Public)
            {
                typeDefinition.IsPublic = true;
            }

            if (preserveSyntaxNodes)
            {
                typeDefinition.SyntaxNode = node;
            }

            DeclareArguments(typeDefinition, node.Arguments, preserveSyntaxNodes);
            DeclareLocations(typeDefinition, node);

            return DirectiveType.CreateUnsafe(typeDefinition);
        }

        private static void DeclareArguments(
            DirectiveTypeDefinition parent,
            IReadOnlyCollection<InputValueDefinitionNode> arguments,
            bool preserveSyntaxNodes)
        {
            foreach (InputValueDefinitionNode argument in arguments)
            {
                var argumentDefinition = new DirectiveArgumentDefinition(
                    argument.Name.Value,
                    argument.Description?.Value,
                    TypeReference.Create(argument.Type),
                    argument.DefaultValue);

                if (preserveSyntaxNodes)
                {
                    argumentDefinition.SyntaxNode = argument;
                }

                parent.Arguments.Add(argumentDefinition);
            }
        }

        private static void DeclareLocations(
            DirectiveTypeDefinition parent,
            DirectiveDefinitionNode node)
        {
            foreach (NameNode location in node.Locations)
            {
                if (Language.DirectiveLocation.TryParse(
                    location.Value,
                    out Language.DirectiveLocation parsedLocation))
                {
                    parent.Locations.Add(MapDirectiveLocation(parsedLocation));
                }
            }
        }

        private static DirectiveLocation MapDirectiveLocation(
            Language.DirectiveLocation location)
        {
            if (!_locs.TryGetValue(location, out DirectiveLocation loc))
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveTypeFactory_LocationNotSupported,
                    location));
            }

            return loc;
        }
    }
}
