using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

internal static class SettingsExtensions
{
    extension(CompositionSettings compositionSettings)
    {
        public CompositionSettings MergeInto(CompositionSettings settings)
        {
            return new CompositionSettings
            {
                Merger = new CompositionSettings.MergerSettings
                {
                    AddFusionDefinitions =
                        compositionSettings.Merger.AddFusionDefinitions
                        ?? settings.Merger.AddFusionDefinitions,
                    CacheControlMergeBehavior =
                        compositionSettings.Merger.CacheControlMergeBehavior
                        ?? settings.Merger.CacheControlMergeBehavior,
                    EnableGlobalObjectIdentification =
                        compositionSettings.Merger.EnableGlobalObjectIdentification
                        ?? settings.Merger.EnableGlobalObjectIdentification,
                    RemoveUnreferencedDefinitions =
                        compositionSettings.Merger.RemoveUnreferencedDefinitions
                        ?? settings.Merger.RemoveUnreferencedDefinitions,
                    TagMergeBehavior =
                        compositionSettings.Merger.TagMergeBehavior
                        ?? settings.Merger.TagMergeBehavior
                },
                Satisfiability = new CompositionSettings.SatisfiabilitySettings
                {
                    IncludeSatisfiabilityPaths =
                        compositionSettings.Satisfiability.IncludeSatisfiabilityPaths
                        ?? settings.Satisfiability.IncludeSatisfiabilityPaths
                }
            };
        }
    }

    extension(CompositionSettings.PreprocessorSettings preprocessorSettings)
    {
        public void MergeInto(SourceSchemaPreprocessorOptions preprocessorOptions)
        {
            if (preprocessorSettings.ExcludeByTag is { } excludeByTag)
            {
                preprocessorOptions.ExcludeByTag.UnionWith(excludeByTag);
            }
        }
    }

    extension(CompositionSettings.MergerSettings mergerSettings)
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

    extension(CompositionSettings.SatisfiabilitySettings satisfiabilitySettings)
    {
        public SatisfiabilityOptions ToOptions()
        {
            var satisfiabilityOptions = new SatisfiabilityOptions();

            if (satisfiabilitySettings.IncludeSatisfiabilityPaths is { } includeSatisfiabilityPaths)
            {
                satisfiabilityOptions.IncludeSatisfiabilityPaths = includeSatisfiabilityPaths;
            }

            return satisfiabilityOptions;
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

    extension(SourceSchemaSettings.ParserSettings parserSettings)
    {
        private SourceSchemaParserOptions ToOptions()
        {
            var parserOptions = new SourceSchemaParserOptions();

            if (parserSettings.EnableSchemaValidation is { } enableSchemaValidation)
            {
                parserOptions.EnableSchemaValidation = enableSchemaValidation;
            }

            return parserOptions;
        }
    }

    extension(SourceSchemaSettings.PreprocessorSettings preprocessorSettings)
    {
        private SourceSchemaPreprocessorOptions ToOptions()
        {
            var preprocessorOptions = new SourceSchemaPreprocessorOptions();

            if (preprocessorSettings.EnableSchemaValidation is { } enableSchemaValidation)
            {
                preprocessorOptions.EnableSchemaValidation = enableSchemaValidation;
            }

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

    extension(SourceSchemaSettings.SatisfiabilitySettings satisfiabilitySettings)
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
}
