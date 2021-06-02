using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNet.Globbing;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Language;
using Microsoft.CodeAnalysis;
using static System.IO.Path;

namespace HotChocolate.Analyzers
{
    public static class Files
    {
        public const string GraphQLExtension = ".graphql";

        public static IReadOnlyList<GraphQLConfig> GetConfigurations(
            this GeneratorExecutionContext context)
        {
            var list = new List<GraphQLConfig>();

            foreach (var configLocation in GetConfigurationFiles(context))
            {
                try
                {
                    string json = File.ReadAllText(configLocation);
                    GraphQLConfig config = GraphQLConfig.FromJson(json);
                    config.Location = configLocation;
                    list.Add(config);
                }
                catch (Exception ex)
                {
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage(ex.Message)
                            .SetException(ex)
                            .SetExtension(ErrorHelper.File, configLocation)
                            .AddLocation(new Location(1, 1))
                            .Build());
                }
            }

            return list;
        }

        private static IReadOnlyList<string> GetConfigurationFiles(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => GetFileName(t).Equals(
                    FileNames.GraphQLConfigFile,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        public static IReadOnlyList<DocumentNode> GetSchemaDocuments(
            this GeneratorExecutionContext context,
            GraphQLConfig config)
        {
            var list = new List<DocumentNode>();

            var rootDirectory = GetDirectoryName(config.Location) + DirectorySeparatorChar;
            var glob = Glob.Parse(config.Documents);

            foreach (string file in context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => GetExtension(t).Equals(
                    GraphQLExtension,
                    StringComparison.OrdinalIgnoreCase))
                .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t)))
            {
                try
                {
                    DocumentNode document = Utf8GraphQLParser.Parse(File.ReadAllBytes(file));

                    if (!document.Definitions.OfType<IExecutableDefinitionNode>().Any())
                    {
                        list.Add(document);
                    }
                }
                catch (SyntaxException ex)
                {
                    context.ReportError(ex);
                }
            }

            return list;
        }
    }
}
