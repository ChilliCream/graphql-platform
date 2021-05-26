using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using IOPath = System.IO.Path;

namespace HotChocolate.Data.Neo4J.Analyzers
{
    public static class Files
    {
        public static IReadOnlyList<string> GetGraphQLFiles(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Distinct()
                .Where(t => IOPath.GetExtension(t).Equals(
                    ".graphql",
                    StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText)
                .ToList();
    }
}
