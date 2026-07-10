using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
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
    private ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> _directiveExtensions =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, IReadOnlyList<DirectiveNode>>.Empty;
#endif
    private readonly HashSet<string> _appliedDirectiveExtensions = [];

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        if (context.Features.Get<TypeSystemFeature>() is { } feature)
        {
            if (feature.ScalarDirectives is { Count: > 0 } scalarDirectives)
            {
                _directives = scalarDirectives;
            }

            if (feature.DirectiveExtensions is { Count: > 0 } directiveExtensions)
            {
                _directiveExtensions = directiveExtensions;
            }
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
                        scalarTypeConfig.SpecifiedBy = url.Value;
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

        if (_directiveExtensions.TryGetValue(completionContext.Type.Name, out var extensionDirectives)
            && configuration is DirectiveTypeConfiguration directiveTypeConfig)
        {
            foreach (var directive in extensionDirectives)
            {
                if (directive.IsDeprecationReason())
                {
                    directiveTypeConfig.DeprecationReason = directive.DeprecationReason();
                    continue;
                }

                directiveTypeConfig.Directives.Add(new DirectiveConfiguration(directive));
                ((RegisteredType)completionContext).Dependencies.Add(
                    new TypeDependency(
                        TypeReference.CreateDirective(directive.Name.Value),
                        TypeDependencyFulfilled.Completed));
            }

            _appliedDirectiveExtensions.Add(completionContext.Type.Name);
        }
    }

    public override void OnTypesCompletedName()
    {
        foreach (var (name, _) in _directiveExtensions)
        {
            if (!_appliedDirectiveExtensions.Contains(name))
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            TypeResources.DirectiveExtension_UnknownTarget,
                            name)
                        .SetCode(ErrorCodes.Schema.DirectiveExtensionUnknownTarget)
                        .Build());
            }
        }
    }
}
