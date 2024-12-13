using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

public sealed class SchemaComposer
{
    public CompositionResult<SchemaDefinition> Compose(
        IEnumerable<SchemaDefinition> schemaDefinitions,
        ICompositionLog compositionLog)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinitions);
        ArgumentNullException.ThrowIfNull(compositionLog);

        var context = new CompositionContext([.. schemaDefinitions], compositionLog);

        // Validate Source Schemas
        var validationResult = new SourceSchemaValidator().Validate(context);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // Merge Source Schemas
        var mergeResult = new SourceSchemaMerger().Merge(context);

        if (mergeResult.IsFailure)
        {
            return mergeResult;
        }

        // Validate Satisfiability
        var satisfiabilityResult = new SatisfiabilityValidator().Validate(mergeResult.Value);

        if (satisfiabilityResult.IsFailure)
        {
            return satisfiabilityResult;
        }

        return mergeResult;
    }
}
