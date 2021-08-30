using System;
using System.IO;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    internal static class SchemaLoader
    {
        private static readonly Assembly _currentAssembly = typeof(SchemaLoader).Assembly;

        internal static DocumentNode GetDocumentFromFile(
            string @namespace,
            string fileName)
        {
            var resourceName = $"{@namespace}.{fileName}.graphql";

            string content;
            try
            {
                using Stream? stream = _currentAssembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new ArgumentException($"Couldn't read stream of embedded resource '{resourceName}'.");
                }

                using var streamReader = new StreamReader(stream);
                content = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Couldn't read embedded resource '{resourceName}'.", ex);
            }

            return Utf8GraphQLParser.Parse(content);
        }
    }
}
