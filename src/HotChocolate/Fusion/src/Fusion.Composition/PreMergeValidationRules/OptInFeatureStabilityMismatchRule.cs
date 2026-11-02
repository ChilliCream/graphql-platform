using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using DirectiveNames = HotChocolate.Types.DirectiveNames;

namespace HotChocolate.Fusion.PreMergeValidationRules;

internal sealed class OptInFeatureStabilityMismatchRule : IEventHandler<SchemaGroupEvent>
{
    public void Handle(SchemaGroupEvent @event, CompositionContext context)
    {
        MultiValueDictionary<string, (string Stability, MutableSchemaDefinition Schema)>
            declarationsByFeature = [];

        foreach (var schema in @event.Schemas)
        {
            foreach (var directive in schema.Directives[DirectiveNames.OptInFeatureStability.Name])
            {
                var parsed = OptInFeatureStabilityDirective.From(directive);
                declarationsByFeature.Add(parsed.Feature, (parsed.Stability, schema));
            }
        }

        foreach (var (feature, declarations) in declarationsByFeature)
        {
            var group = declarations.ToArray();

            for (var i = 0; i < group.Length - 1; i++)
            {
                var a = group[i];
                var b = group[i + 1];

                if (!string.Equals(a.Stability, b.Stability, StringComparison.Ordinal))
                {
                    context.Log.Write(
                        OptInFeatureStabilityMismatch(
                            feature,
                            a.Schema,
                            a.Stability,
                            b.Schema,
                            b.Stability));
                }
            }
        }
    }
}
