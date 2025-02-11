using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaParser(IEnumerable<string> sourceSchemas, ICompositionLog log)
{
    public CompositionResult<ImmutableSortedSet<SchemaDefinition>> Parse()
    {
        var sortedSetBuilder = ImmutableSortedSet.CreateBuilder(new SchemaByNameComparer());

        foreach (var sourceSchema in sourceSchemas)
        {
            try
            {
                sortedSetBuilder.Add(SchemaParser.Parse(sourceSchema));
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
