using ChilliCream.Nitro.CommandLine.Fusion.Settings;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Fusion;

internal static class SettingsExtensions
{
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
}
