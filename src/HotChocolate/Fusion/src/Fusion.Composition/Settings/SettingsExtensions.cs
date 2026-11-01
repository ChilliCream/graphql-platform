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
                    DefaultListSize =
                        compositionSettings.Merger.DefaultListSize
                        ?? settings.Merger.DefaultListSize,
                    MaxFieldCost =
                        compositionSettings.Merger.MaxFieldCost
                        ?? settings.Merger.MaxFieldCost,
                    MaxTypeCost =
                        compositionSettings.Merger.MaxTypeCost
                        ?? settings.Merger.MaxTypeCost,
                    EnforceCostLimits =
                        compositionSettings.Merger.EnforceCostLimits
                        ?? settings.Merger.EnforceCostLimits,
                    SkipCostAnalyzer =
                        compositionSettings.Merger.SkipCostAnalyzer
                        ?? settings.Merger.SkipCostAnalyzer,
                    MaxResponseSize =
                        compositionSettings.Merger.MaxResponseSize
                        ?? settings.Merger.MaxResponseSize,
                    CostPlanCacheSize =
                        compositionSettings.Merger.CostPlanCacheSize
                        ?? settings.Merger.CostPlanCacheSize,
                    CaseBudget =
                        compositionSettings.Merger.CaseBudget
                        ?? settings.Merger.CaseBudget,
                    CaseBudgetExceededBehavior =
                        compositionSettings.Merger.CaseBudgetExceededBehavior
                        ?? settings.Merger.CaseBudgetExceededBehavior,
                    EnableGlobalObjectIdentification =
                        compositionSettings.Merger.EnableGlobalObjectIdentification
                        ?? settings.Merger.EnableGlobalObjectIdentification,
                    EnumValuesMergeBehavior =
                        compositionSettings.Merger.EnumValuesMergeBehavior
                        ?? settings.Merger.EnumValuesMergeBehavior,
                    NodeResolution =
                        compositionSettings.Merger.NodeResolution
                        ?? settings.Merger.NodeResolution,
                    RemoveUnreferencedDefinitions =
                        compositionSettings.Merger.RemoveUnreferencedDefinitions
                        ?? settings.Merger.RemoveUnreferencedDefinitions,
                    TagMergeBehavior =
                        compositionSettings.Merger.TagMergeBehavior
                        ?? settings.Merger.TagMergeBehavior
                },
                Preprocessor = new CompositionSettings.PreprocessorSettings
                {
                    ExcludeByTag =
                        compositionSettings.Preprocessor.ExcludeByTag
                        ?? settings.Preprocessor.ExcludeByTag
                },
                Satisfiability = new CompositionSettings.SatisfiabilitySettings
                {
                    IncludeSatisfiabilityPaths =
                        compositionSettings.Satisfiability.IncludeSatisfiabilityPaths
                        ?? settings.Satisfiability.IncludeSatisfiabilityPaths
                },
                ApolloFederationCompatibility =
                    new CompositionSettings.ApolloFederationCompatibilitySettings
                    {
                        AllowNonResolvableInterfaceObjects =
                            compositionSettings.ApolloFederationCompatibility
                                .AllowNonResolvableInterfaceObjects
                            ?? settings.ApolloFederationCompatibility
                                .AllowNonResolvableInterfaceObjects,
                        ShareableFieldRuntimeTypeRouting =
                            compositionSettings.ApolloFederationCompatibility
                                .ShareableFieldRuntimeTypeRouting
                            ?? settings.ApolloFederationCompatibility
                                .ShareableFieldRuntimeTypeRouting
                    }
            };
        }
    }

    extension(CompositionSettings.ApolloFederationCompatibilitySettings compatibilitySettings)
    {
        public ApolloFederationCompatibilityOptions ToOptions()
        {
            var options = new ApolloFederationCompatibilityOptions();

            if (compatibilitySettings.AllowNonResolvableInterfaceObjects is { } allow)
            {
                options.AllowNonResolvableInterfaceObjects = allow;
            }

            if (compatibilitySettings.ShareableFieldRuntimeTypeRouting is { } routing)
            {
                options.ShareableFieldRuntimeTypeRouting = routing;
            }

            return options;
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

            if (mergerSettings.DefaultListSize is { } defaultListSize)
            {
                mergerOptions.DefaultListSize = defaultListSize;
            }

            if (mergerSettings.MaxFieldCost is { } maxFieldCost)
            {
                mergerOptions.MaxFieldCost = maxFieldCost;
            }

            if (mergerSettings.MaxTypeCost is { } maxTypeCost)
            {
                mergerOptions.MaxTypeCost = maxTypeCost;
            }

            if (mergerSettings.EnforceCostLimits is { } enforceCostLimits)
            {
                mergerOptions.EnforceCostLimits = enforceCostLimits;
            }

            if (mergerSettings.SkipCostAnalyzer is { } skipCostAnalyzer)
            {
                mergerOptions.SkipCostAnalyzer = skipCostAnalyzer;
            }

            if (mergerSettings.MaxResponseSize is { } maxResponseSize)
            {
                mergerOptions.MaxResponseSize = maxResponseSize;
            }

            if (mergerSettings.CostPlanCacheSize is { } costPlanCacheSize)
            {
                mergerOptions.CostPlanCacheSize = costPlanCacheSize;
            }

            if (mergerSettings.CaseBudget is { } caseBudget)
            {
                mergerOptions.CaseBudget = caseBudget;
            }

            if (mergerSettings.CaseBudgetExceededBehavior is { } caseBudgetExceededBehavior)
            {
                mergerOptions.CaseBudgetExceededBehavior = caseBudgetExceededBehavior;
            }

            if (mergerSettings.EnableGlobalObjectIdentification is { } enableGlobalObjectIdentification)
            {
                mergerOptions.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
            }

            if (mergerSettings.EnumValuesMergeBehavior is { } enumValuesMergeBehavior)
            {
                mergerOptions.EnumValuesMergeBehavior = enumValuesMergeBehavior;
            }

            if (mergerSettings.NodeResolution is { } nodeResolution)
            {
                mergerOptions.NodeResolution = nodeResolution;
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

            if (preprocessorSettings.InferShareable is { } inferShareable)
            {
                preprocessorOptions.InferShareable = inferShareable;
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
