using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    internal class CostTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is ObjectTypeDefinition objectDef)
            {
                foreach (ObjectFieldDefinition field in objectDef.Fields)
                {
                    if (field.GetDirectives() is { Count: > 0 } directives &&
                        directives.All(t => IsCostDirective(t)))
                    {
                        if (field.Resolver is not null)
                        {
                            field.Directives.Add(
                                new DirectiveDefinition(
                                    new DirectiveDefinition(
                                        null,
                                        "cost",)))
                        }


                        MemberInfo? resolver = field.ResolverMember ?? field.Member;

                    }
                }
            }
        }

        private static bool IsCostDirective(DirectiveDefinition directive)
        {
            if (directive.Reference is NameDirectiveReference { Name: { Value: "cost" } })
            {
                return true;
            }

            if (directive.Reference is ClrTypeDirectiveReference { ClrType: { } type } &&
                type == typeof(CostDirective))
            {
                return true;
            }

            return false;
        }
    }
}