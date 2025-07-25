using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaParser(IEnumerable<string> sourceSchemas, ICompositionLog log)
{
    public CompositionResult<ImmutableSortedSet<MutableSchemaDefinition>> Parse()
    {
        var sortedSetBuilder = ImmutableSortedSet.CreateBuilder(
            new SchemaByNameComparer<MutableSchemaDefinition>());

        foreach (var sourceSchema in sourceSchemas)
        {
            try
            {
                var schema = new MutableSchemaDefinition();
                schema.AddBuiltInFusionTypes();
                schema.AddBuiltInFusionDirectives();
                SchemaParser.Parse(
                    schema,
                    sourceSchema,
                    new SchemaParserOptions
                    {
                        IgnoreExistingTypes = true,
                        IgnoreExistingDirectives = true
                    });
                var schemaNameDirective = schema.Directives.FirstOrDefault(SchemaName);

                if (schema.Name == "default"
                    && schemaNameDirective is not null
                    && schemaNameDirective.Arguments.TryGetValue(Value, out var valueArg)
                    && valueArg is StringValueNode valueStringValueNode)
                {
                    schema.Name = valueStringValueNode.Value;
                }

                sortedSetBuilder.Add(schema);
            }
            catch (Exception ex)
            {
                log.Write(LogEntryHelper.InvalidGraphQL(ex.Message));
                break;
            }
        }

        return log.HasErrors
            ? ErrorHelper.SourceSchemaParsingFailed()
            : sortedSetBuilder.ToImmutable();
    }
}
