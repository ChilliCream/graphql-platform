using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using MicrosoftAuthorize = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class MicrosoftAuthorizeAttributeTypeInterceptor : TypeInterceptor
{
    // todo: is this the right method for this?
    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition, IDictionary<string, object?> contextData)
    {
        if (discoveryContext.IsIntrospectionType)
        {
            return;
        }

        if (definition is ObjectTypeDefinition objectTypeDefinition)
        {
            // Get AuthorizeAttribute from original type definition.
            MicrosoftAuthorize[] typeAuthAttributes = objectTypeDefinition.RuntimeType
                .GetCustomAttributes(typeof(MicrosoftAuthorize), false)
                .Cast<MicrosoftAuthorize>()
                .ToArray();

            foreach (MicrosoftAuthorize authAttribute in typeAuthAttributes)
            {
                DirectiveDefinition authorizeDirectiveDefinition = BuildAuthorizeDirectiveDefinition(
                    authAttribute.Policy, authAttribute.Roles);

                objectTypeDefinition.Directives.Add(authorizeDirectiveDefinition);
            }

            foreach (ObjectFieldDefinition field in objectTypeDefinition.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                // Get AUthorizeAttribute from field on object type.
                MicrosoftAuthorize[]? fieldAuthAttributes = field.Member?
                    .GetCustomAttributes(typeof(MicrosoftAuthorize), false)
                    .Cast<MicrosoftAuthorize>()
                    .ToArray();

                if (fieldAuthAttributes?.Any() != true)
                {
                    continue;
                }

                foreach (MicrosoftAuthorize authAttribute in fieldAuthAttributes)
                {
                    DirectiveDefinition authorizeDirectiveDefinition = BuildAuthorizeDirectiveDefinition(
                        authAttribute.Policy, authAttribute.Roles);

                    field.Directives.Add(authorizeDirectiveDefinition);
                }
            }

            // todo: this would happen for each object type at the moment,
            //       is this an issue?
            // Register dependency to the AuthorizeDirectiveType
            IExtendedType directive =
                discoveryContext.TypeInspector.GetType(typeof(AuthorizeDirectiveType));

            discoveryContext.Dependencies.Add(new(
                TypeReference.Create(directive),
                TypeDependencyKind.Completed));
        }
    }

    private static DirectiveDefinition BuildAuthorizeDirectiveDefinition(string? policy, string? rolesString)
    {
        var roles =  rolesString?.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim()).ToArray();
        List<ArgumentNode> arguments = new();

        if (!string.IsNullOrEmpty(policy))
        {
            arguments.Add(new ArgumentNode("policy", new StringValueNode(policy)));
        }

        if (roles is { Length: > 0 })
        {
            var listItems = new IValueNode[roles.Length];

            for (var i = 0; i < roles.Length; i++)
            {
                listItems[i] = new StringValueNode(roles[i]);
            }

            arguments.Add(new ArgumentNode("roles",
                new ListValueNode(listItems)));
        }

        return new DirectiveDefinition(
            new DirectiveNode("authorize", arguments));
    }
}
