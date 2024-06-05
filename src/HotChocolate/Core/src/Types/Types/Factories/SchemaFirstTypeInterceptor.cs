#nullable enable

using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Factories;

internal sealed class SchemaFirstTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, IReadOnlyList<DirectiveNode>> _directives = new();

    public Dictionary<string, IReadOnlyList<DirectiveNode>> Directives => _directives;

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (_directives.TryGetValue(completionContext.Type.Name, out var directives) &&
            definition is ScalarTypeDefinition scalarTypeDef)
        {
            foreach (var directive in directives)
            {
                scalarTypeDef.AddDirective(directive.Name.Value, directive.Arguments);
                ((RegisteredType)completionContext).Dependencies.Add(
                    new TypeDependency(
                        TypeReference.CreateDirective(directive.Name.Value),
                        TypeDependencyFulfilled.Completed));
            }
        }
    }
}
