#nullable enable

using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Factories;

internal sealed class SchemaFirstTypeInterceptor : TypeInterceptor
{
    private ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> _directives =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, IReadOnlyList<DirectiveNode>>.Empty;
#endif

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        if (context.Features.Get<TypeSystemFeature>() is { ScalarDirectives: { Count: > 0 } scalarDirectives })
        {
            _directives = scalarDirectives;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (_directives.TryGetValue(completionContext.Type.Name, out var directives)
            && configuration is ScalarTypeConfiguration scalarTypeConfig)
        {
            foreach (var directive in directives)
            {
                if (directive.Name.Value.EqualsOrdinal(DirectiveNames.SpecifiedBy.Name))
                {
                    if (directive.Arguments.Count == 1
                        && directive.Arguments[0].Name.Value.EqualsOrdinal(DirectiveNames.SpecifiedBy.Arguments.Url)
                        && directive.Arguments[0].Value is StringValueNode url)
                    {
                        scalarTypeConfig.SpecifiedBy = new Uri(url.Value);
                        continue;
                    }

                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("The @specifiedBy directive must have exactly one argument called url.")
                            .SetTypeSystemObject(completionContext.Type)
                            .AddSyntaxNode(directive)
                            .Build());
                }

                scalarTypeConfig.AddDirective(directive.Name.Value, directive.Arguments);
                ((RegisteredType)completionContext).Dependencies.Add(
                    new TypeDependency(
                        TypeReference.CreateDirective(directive.Name.Value),
                        TypeDependencyFulfilled.Completed));
            }
        }
    }
}
