using ChilliCream.Nitro.CommandLine.Settings;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine;

internal static class SettingsExtensions
{
    extension(CompositionSettings compositionSettings)
    {
        public CompositionSettings MergeInto(CompositionSettings settings)
        {
            return new CompositionSettings
            {
                Merger = new MergerSettings
                {
                    AddFusionDefinitions =
                        compositionSettings.Merger?.AddFusionDefinitions
                        ?? settings.Merger?.AddFusionDefinitions,
                    CacheControlMergeBehavior =
                        compositionSettings.Merger?.CacheControlMergeBehavior
                        ?? settings.Merger?.CacheControlMergeBehavior,
                    EnableGlobalObjectIdentification =
                        compositionSettings.Merger?.EnableGlobalObjectIdentification
                        ?? settings.Merger?.EnableGlobalObjectIdentification,
                    RemoveUnreferencedDefinitions =
                        compositionSettings.Merger?.RemoveUnreferencedDefinitions
                        ?? settings.Merger?.RemoveUnreferencedDefinitions,
                    TagMergeBehavior =
                        compositionSettings.Merger?.TagMergeBehavior
                        ?? settings.Merger?.TagMergeBehavior
                }
            };
        }
    }

    extension(MergerSettings mergerSettings)
    {
        public SourceSchemaMergerOptions ToOptions()
        {
            var mergerOptions = new SourceSchemaMergerOptions();

            if (mergerSettings.AddFusionDefinitions is { } addFusionDefinitions)
            {
                mergerOptions.AddFusionDefinitions = addFusionDefinitions;
            }

            if (mergerSettings.CacheControlMergeBehavior is { } cacheControlMergeBehavior)
            {
                mergerOptions.CacheControlMergeBehavior = cacheControlMergeBehavior;
            }

            if (mergerSettings.EnableGlobalObjectIdentification is { } enableGlobalObjectIdentification)
            {
                mergerOptions.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
            }

            if (mergerSettings.RemoveUnreferencedDefinitions is { } removeUnreferencedDefinitions)
            {
                mergerOptions.RemoveUnreferencedDefinitions = removeUnreferencedDefinitions;
            }

            if (mergerSettings.TagMergeBehavior is { } tagMergeBehavior)
            {
                mergerOptions.TagMergeBehavior = tagMergeBehavior;
            }

            return mergerOptions;
        }
    }

    extension(ParserSettings parserSettings)
    {
        public SourceSchemaParserOptions ToOptions()
        {
            var parserOptions = new SourceSchemaParserOptions();

            if (parserSettings.EnableSchemaValidation is { } enableSchemaValidation)
            {
                parserOptions.EnableSchemaValidation = enableSchemaValidation;
            }

            return parserOptions;
        }
    }

    extension(PreprocessorSettings preprocessorSettings)
    {
        public SourceSchemaPreprocessorOptions ToOptions()
        {
            var preprocessorOptions = new SourceSchemaPreprocessorOptions();

            if (preprocessorSettings.InferKeysFromLookups is { } inferKeys)
            {
                preprocessorOptions.InferKeysFromLookups = inferKeys;
            }

            if (preprocessorSettings.InheritInterfaceKeys is { } inheritInterfaceKeys)
            {
                preprocessorOptions.InheritInterfaceKeys = inheritInterfaceKeys;
            }

            return preprocessorOptions;
        }
    }

    extension(SatisfiabilitySettings satisfiabilitySettings)
    {
        public void MergeInto(SatisfiabilityOptions satisfiabilityOptions)
        {
            if (satisfiabilitySettings.IgnoredNonAccessibleFields is not { } ignoredNonAccessibleFields)
            {
                return;
            }

            foreach (var (fieldName, paths) in ignoredNonAccessibleFields)
            {
                if (!satisfiabilityOptions.IgnoredNonAccessibleFields.TryGetValue(fieldName, out var existingPaths))
                {
                    existingPaths = [];
                    satisfiabilityOptions.IgnoredNonAccessibleFields[fieldName] = existingPaths;
                }

                foreach (var path in paths)
                {
                    if (!existingPaths.Contains(path))
                    {
                        existingPaths.Add(path);
                    }
                }
            }
        }
    }

    extension(SourceSchemaSettings sourceSchemaSettings)
    {
        public SourceSchemaOptions ToOptions()
        {
            var sourceSchemaOptions = new SourceSchemaOptions();

            if (sourceSchemaSettings.Version is { } version)
            {
                sourceSchemaOptions.Version = version;
            }

            if (sourceSchemaSettings.Parser is { } parserSettings)
            {
                sourceSchemaOptions.Parser = parserSettings.ToOptions();
            }

            if (sourceSchemaSettings.Preprocessor is { } preprocessorSettings)
            {
                sourceSchemaOptions.Preprocessor = preprocessorSettings.ToOptions();
            }

            return sourceSchemaOptions;
        }
    }
}
