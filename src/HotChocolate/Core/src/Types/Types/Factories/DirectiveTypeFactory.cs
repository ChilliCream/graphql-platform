using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories;

internal sealed class DirectiveTypeFactory : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
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
            {
                Language.DirectiveLocation.VariableDefinition,
                DirectiveLocation.VariableDefinition
            }
        };

    public DirectiveType Create(IDescriptorContext context, DirectiveDefinitionNode node)
    {
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new DirectiveTypeDefinition(
            node.Name.Value,
            node.Description?.Value,
            isRepeatable: node.IsRepeatable);

        if (context.Options.DefaultDirectiveVisibility is DirectiveVisibility.Public)
        {
            typeDefinition.IsPublic = true;
        }

        DeclareArguments(context, typeDefinition, node.Arguments, path);
        DeclareLocations(typeDefinition, node);

        return DirectiveType.CreateUnsafe(typeDefinition);
    }

    private static void DeclareArguments(
        IDescriptorContext context,
        DirectiveTypeDefinition parent,
        IReadOnlyCollection<InputValueDefinitionNode> arguments,
        Stack<IDefinition> path)
    {
        path.Push(parent);

        foreach (var argument in arguments)
        {
            var argumentDefinition = new DirectiveArgumentDefinition(
                argument.Name.Value,
                argument.Description?.Value,
                TypeReference.Create(argument.Type),
                argument.DefaultValue);

            if (argument.DeprecationReason() is { Length: > 0, } reason)
            {
                argumentDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, argumentDefinition, argument, path);

            parent.Arguments.Add(argumentDefinition);
        }

        path.Pop();
    }

    private static void DeclareLocations(
        DirectiveTypeDefinition parent,
        DirectiveDefinitionNode node)
    {
        foreach (var location in node.Locations)
        {
            if (Language.DirectiveLocation.TryParse(
                location.Value,
                out var parsedLocation))
            {
                parent.Locations |= MapDirectiveLocation(parsedLocation);
            }
        }
    }

    private static DirectiveLocation MapDirectiveLocation(
        Language.DirectiveLocation location)
    {
        if (!_locs.TryGetValue(location, out var loc))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                TypeResources.DirectiveTypeFactory_LocationNotSupported,
                location));
        }

        return loc;
    }
}
