namespace HotChocolate.Fusion;

public sealed class CompositionApiTests
{
    [Fact]
    public void CompositionAssembly_Should_ExposeSupportedModels_When_InspectingPublicTypes()
    {
        var exportedTypeNames = typeof(SchemaComposer)
            .Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, exportedTypeNames).MatchInlineSnapshot(
            """
            HotChocolate.Fusion.Composition.SchemaCompositionException
            HotChocolate.Fusion.Directives.CacheControlScope
            HotChocolate.Fusion.Errors.CompositionError
            HotChocolate.Fusion.Logging.CompositionLog
            HotChocolate.Fusion.Logging.Contracts.ICompositionLog
            HotChocolate.Fusion.Logging.LogEntry
            HotChocolate.Fusion.Logging.LogEntryBuilder
            HotChocolate.Fusion.Logging.LogEntryCodes
            HotChocolate.Fusion.Logging.LogSeverity
            HotChocolate.Fusion.Options.ApolloFederationCompatibilityOptions
            HotChocolate.Fusion.Options.DirectiveMergeBehavior
            HotChocolate.Fusion.Options.SatisfiabilityOptions
            HotChocolate.Fusion.Options.SchemaComposerOptions
            HotChocolate.Fusion.Options.SourceSchemaMergerOptions
            HotChocolate.Fusion.Options.SourceSchemaOptions
            HotChocolate.Fusion.Options.SourceSchemaParserOptions
            HotChocolate.Fusion.Options.SourceSchemaPreprocessorOptions
            HotChocolate.Fusion.Results.CompositionResult
            HotChocolate.Fusion.Results.CompositionResult`1
            HotChocolate.Fusion.WellKnownSourceSchemaSettings
            """);
    }
}
