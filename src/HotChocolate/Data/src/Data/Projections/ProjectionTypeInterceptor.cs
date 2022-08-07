using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.Projections.ProjectionConvention;

namespace HotChocolate.Data.Projections;

public class ProjectionTypeInterceptor : TypeInterceptor
{
    public override bool CanHandle(ITypeSystemObjectContext context) => true;

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is ObjectTypeDefinition objectTypeDefinition)
        {
            List<string>? alwaysProjected = null;
            foreach (var field in objectTypeDefinition.Fields)
            {
                alwaysProjected ??= new List<string>();
                if (field.GetContextData().TryGetValue(IsProjectedKey, out var value) &&
                    value is true)
                {
                    alwaysProjected.Add(field.Name);
                }
            }

            if (alwaysProjected?.Count > 0)
            {
                definition.ContextData[AlwaysProjectedFieldsKey] = alwaysProjected.ToArray();
            }
        }
    }
}
