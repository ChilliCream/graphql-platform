#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Factories;

internal sealed class SchemaFirstTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, IReadOnlyList<DirectiveNode>> _directives = new();

    public Dictionary<string, IReadOnlyList<DirectiveNode>> Directives => _directives;

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (_directives.TryGetValue(completionContext.Type.Name, out var directives)
            && definition is ScalarTypeDefinition scalarTypeDef)
        {
            foreach (var directive in directives)
            {
                if (directive.Name.Value.EqualsOrdinal(SpecifiedByDirectiveType.Names.SpecifiedBy))
                {
                    if (directive.Arguments.Count == 1
                        && directive.Arguments[0].Name.Value.EqualsOrdinal("url")
                        && directive.Arguments[0].Value is StringValueNode url)
                    {
                        scalarTypeDef.SpecifiedBy = new Uri(url.Value);
                        continue;
                    }

                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("The @specifiedBy directive must have exactly one argument called url.")
                            .SetTypeSystemObject(completionContext.Type)
                            .AddSyntaxNode(directive)
                            .Build());
                }

                scalarTypeDef.AddDirective(directive.Name.Value, directive.Arguments);
                ((RegisteredType)completionContext).Dependencies.Add(
                    new TypeDependency(
                        TypeReference.CreateDirective(directive.Name.Value),
                        TypeDependencyFulfilled.Completed));
            }
        }
    }
}
