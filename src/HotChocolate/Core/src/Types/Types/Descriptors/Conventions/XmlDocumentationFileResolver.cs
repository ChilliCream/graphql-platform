using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace HotChocolate.Types.Descriptors
{
    public class XmlDocumentationFileResolver : IXmlDocumentationFileResolver
    {
        private const string _bin = "bin";

        private readonly ConcurrentDictionary<string, XDocument> _cache =
            new ConcurrentDictionary<string, XDocument>(
                StringComparer.OrdinalIgnoreCase);

        public bool TryGetXmlDocument(Assembly assembly, out XDocument document)
        {
            var fullName = assembly.GetName().FullName;

            if (!_cache.TryGetValue(fullName, out XDocument doc))
            {
                var pathToXmlFile = GetXmlDocumentationPath(assembly);

                if (File.Exists(pathToXmlFile))
                {
                    doc = XDocument.Load(
                        pathToXmlFile,
                        LoadOptions.PreserveWhitespace);
                    _cache[fullName] = doc;
                }
            }

            document = doc;
            return document != null;
        }

        private string GetXmlDocumentationPath(Assembly assembly)
        {
            try
            {
                if (assembly is null)
                {
                    return null;
                }

                AssemblyName assemblyName = assembly.GetName();
                if (string.IsNullOrEmpty(assemblyName.Name))
                {
                    return null;
                }

                if (_cache.ContainsKey(assemblyName.FullName))
                {
                    return null;
                }

                var expectedDocFile = $"{assemblyName.Name}.xml";

                string path;
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    var assemblyDirectory =
                        IOPath.GetDirectoryName(assembly.Location);
                    path = IOPath.Combine(assemblyDirectory, expectedDocFile);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                var codeBase = assembly.CodeBase;
                if (!string.IsNullOrEmpty(codeBase))
                {
                    path = IOPath.Combine(
                        IOPath.GetDirectoryName(
                            codeBase.Replace("file:///", string.Empty)),
                        expectedDocFile)
                        .Replace("file:\\", string.Empty);

                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    path = IOPath.Combine(baseDirectory, expectedDocFile);
                    if (File.Exists(path))
                    {
                        return path;
                    }

                    return IOPath.Combine(
                        baseDirectory,
                        _bin,
                        expectedDocFile);
                }

                var currentDirectory = Directory.GetCurrentDirectory();
                path = IOPath.Combine(currentDirectory, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }

                path = IOPath.Combine(
                    currentDirectory,
                    _bin,
                    expectedDocFile);

                if (File.Exists(path))
                {
                    return path;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
