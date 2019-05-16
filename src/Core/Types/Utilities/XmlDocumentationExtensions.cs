using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace HotChocolate.Utilities
{
    public interface IXmlDocumentationFileResolver
    {
        bool TryGetXmlDocument(
            AssemblyName assemblyName,
            out XDocument document);
    }

    public class XmlDocumentationFileResolver
        : IXmlDocumentationFileResolver
    {
        private readonly ConcurrentDictionary<string, XDocument> _cache =
            new ConcurrentDictionary<string, XDocument>(
                StringComparer.OrdinalIgnoreCase);

        public bool TryGetXmlDocument(
            AssemblyName assemblyName,
            out XDocument document)
        {
            if (!_cache.TryGetValue(assemblyName.FullName, out XDocument doc))
            {
                if (!File.Exists(pathToXmlFile))
                {
                    doc = _cache[assemblyName.FullName] = null;
                }
                else
                {
                    doc = XDocument.Load(
                        pathToXmlFile,
                        LoadOptions.PreserveWhitespace);
                    _cache[assemblyName.FullName] = doc;
                }
            }

            document = doc;
            return document != null;
        }

        private static string GetXmlDocumentationPath(Assembly assembly)
        {
            try
            {
                if (assembly == null)
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
                    string assemblyDirectory =
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

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
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

                string currentDirectory = Directory.GetCurrentDirectory();
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
