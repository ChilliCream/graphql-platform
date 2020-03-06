using System;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Language;
using MarshmallowPie.Processing;

namespace MarshmallowPie.BackgroundServices
{
    public sealed class RelayDocument
    {
        private RelayDocument(string name, IReadOnlyList<RelayQuery> queries)
        {
            Name = name;
            Queries = queries;
        }

        public string Name { get; }

        public IReadOnlyList<RelayQuery> Queries { get; }

        public static RelayDocument Parse(DocumentInfo documentInfo, string sourceText)
        {
            if (documentInfo is null)
            {
                throw new ArgumentNullException(nameof(documentInfo));
            }

            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    "The source text mustn't be null or empty.",
                    nameof(sourceText));
            }

            HashFormat format = documentInfo.HashFormat ?? HashFormat.Hex;
            string algorithm = documentInfo.HashAlgorithm?.ToUpperInvariant() ?? "MD5";
            var queries = new List<RelayQuery>();

            foreach (var query in Parse(sourceText))
            {
                queries.Add(new RelayQuery(
                    $"{documentInfo.Name}_{query.Item1}",
                    new DocumentHash(
                        query.hash,
                        algorithm,
                        format),
                    query.sourceText));
            }

            return new RelayDocument(documentInfo.Name, queries);
        }

        private static IEnumerable<(string hash, string sourceText)> Parse(
            string sourceText)
        {
            using (var document = JsonDocument.Parse(sourceText))
            {
                foreach (JsonProperty query in document.RootElement.EnumerateObject())
                {
                    yield return (query.Name, query.Value.GetString());
                }
            }
        }
    }
}
