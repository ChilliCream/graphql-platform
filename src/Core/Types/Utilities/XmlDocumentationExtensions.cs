using System.Globalization;
using System.Xml.Xsl.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using IOPath = System.IO.Path;

namespace HotChocolate.Utilities
{
    /// <summary>
    /// Provides extension methods for reading XML comments
    /// from reflected members.
    /// </summary>
    /// <remarks>
    /// This class currently works only on the desktop .NET framework.
    /// </remarks>
    internal static class XmlDocumentationExtensions
    {
        private const string _bin = "bin";
        private const string _inheritdoc = "inheritdoc";


        private static readonly ConcurrentDictionary<string, XDocument> _cache =
            new ConcurrentDictionary<string, XDocument>(
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the contents of the "summary" XML documentation
        /// tag for the specified member.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The contents of the "summary" tag for the member.
        /// </returns>
        internal static string GetXmlSummary(this Type type)
        {
            return GetXmlDocumentationTagAsync(type.GetTypeInfo(), "summary");
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<string> GetXmlSummaryAsync(this MemberInfo member)
        {
            return await GetXmlDocumentationTagAsync(member, "summary").ConfigureAwait(false);
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        internal static string GetXmlDocumentation(this ParameterInfo parameter)
        {
            var assemblyName = parameter.Member.Module.Assembly.GetName();
            if (IgnoreAssembly(assemblyName))
            {
                return string.Empty;
            }

            var documentationPath = GetXmlDocumentationPath(parameter.Member.Module.Assembly);
            var element = GetXmlDocumentation(parameter, documentationPath);
            return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element));
        }

        /// <summary>Clears the cache.</summary>
        /// <returns>The task.</returns>
        internal static Task ClearCacheAsync()
        {
            using (Lock.Lock())
            {
                _cache.Clear();
                return Task.CompletedTask;
            }
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        private static Task<string> GetXmlDocumentationTagAsync(this Type type, string tagName)
        {
            return GetXmlDocumentationTagAsync((MemberInfo)type.GetTypeInfo(), tagName);
        }

        /// <summary>Converts the given XML documentation <see cref="XElement"/> to text.</summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The text</returns>
        private static string GetXmlDocumentationText(this XElement element)
        {
            if (element == null)
            {
                return null;
            }

            var value = new StringBuilder();
            foreach (var node in element.Nodes())
            {
                var currentElement = node as XElement;
                if (currentElement == null)
                {
                    value.Append(node);
                    continue;
                }

                if (currentElement.Name != "see")
                {
                    value.Append(currentElement.Value);
                    continue;
                }

                var attribute = currentElement.Attribute("langword");
                if (attribute != null)
                {
                    value.Append(attribute.Value);
                    continue;
                }

                if (!string.IsNullOrEmpty(currentElement.Value))
                {
                    value.Append(currentElement.Value);
                }
                else
                {
                    attribute = currentElement.Attribute("cref");
                    if (attribute != null)
                    {
                        value.Append(attribute.Value.Trim('!', ':').Trim().Split('.').Last());
                    }
                    else
                    {
                        attribute = currentElement.Attribute("href");
                        if (attribute != null)
                        {
                            value.Append(attribute.Value);
                        }
                    }
                }
            }

            return value.ToString();
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        private static async Task<string> GetXmlDocumentationTagAsync(this MemberInfo member, string tagName)
        {
            var assemblyName = member.Module.Assembly.GetName();
            using (Lock.Lock())
            {
                if (IgnoreAssembly(assemblyName))
                {
                    return string.Empty;
                }

                var documentationPath = GetXmlDocumentationPath(member.Module.Assembly);
                var element = await GetXmlDocumentation(member, documentationPath).ConfigureAwait(false);
                return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element?.Element(tagName)));
            }
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        private static XElement GetXmlDocumentationAsync(this MemberInfo member)
        {
            return GetXmlDocumentation(member);
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        private static async Task<XElement> GetXmlDocumentation(this MemberInfo member, string pathToXmlFile)
        {

            return await GetXmlDocumentation(member, pathToXmlFile).ConfigureAwait(false);
        }

        private static XElement GetXmlDocumentation(
            this ParameterInfo parameter,
            string pathToXmlFile)
        {
            try
            {
                var assemblyName = parameter.Member.Module.Assembly.GetName();
                var document = TryGetXmlDocument(assemblyName, pathToXmlFile);
                if (document == null)
                {
                    return null;
                }

                return GetXmlDocumentationAsync(parameter, document);
            }
            catch
            {
                return null;
            }
        }

        private static XElement GetXmlDocumentation(this MemberInfo member)
        {
            var assemblyName = member.Module.Assembly.GetName();
            if (IgnoreAssembly(assemblyName))
            {
                return null;
            }

            var documentationPath = GetXmlDocumentationPath(member.Module.Assembly);
            return GetXmlDocumentation(member, documentationPath);
        }

        private static XElement GetXmlDocumentation(
            this MemberInfo member,
            string pathToXmlFile)
        {
            try
            {
                var assemblyName = member.Module.Assembly.GetName();
                var document = TryGetXmlDocument(assemblyName, pathToXmlFile);
                if (document == null)
                {
                    return null;
                }

                var element = GetMemberDocumentation(member, document);
                ReplaceInheritdocElements(member, element);
                return element;
            }
            catch
            {
                return null;
            }
        }

        private static XDocument TryGetXmlDocument(
            AssemblyName assemblyName,
            string pathToXmlFile)
        {
            if (!_cache.TryGetValue(assemblyName.FullName, out XDocument doc))
            {
                if (!File.Exists(pathToXmlFile))
                {
                    _cache[assemblyName.FullName] = null;
                }
                else
                {
                    doc = XDocument.Load(
                        pathToXmlFile,
                        LoadOptions.PreserveWhitespace);
                    _cache[assemblyName.FullName] = doc;
                }
            }

            return doc;
        }

        private static bool IgnoreAssembly(AssemblyName assemblyName)
        {
            if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
            {
                return true;
            }

            return false;
        }

        private static XElement GetMemberDocumentation(
            this MemberInfo member,
            XDocument xml)
        {
            MemberName name = GetMemberElementName(member);
            return xml.XPathSelectElements(name.Path)
                .FirstOrDefault();
        }

        private static XElement GetParameterDocumentation(
            this ParameterInfo parameter,
            XDocument xml)
        {
            MemberName name = GetMemberElementName(parameter.Member);
            var result = xml.XPathSelectElements(name.Path);

            var element = result.FirstOrDefault();
            if (element == null)
            {
                return null;
            }

            ReplaceInheritdocElements(parameter.Member, element);

            if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
            {
                result = xml.XPathSelectElements(name.ReturnsPath);
            }
            else
            {
                result = xml.XPathSelectElements(
                    name.GetParameterPath(parameter.Name));
            }

            return result.FirstOrDefault();
        }

        private static void ReplaceInheritdocElements(
            this MemberInfo member,
            XElement element)
        {
            if (element == null)
            {
                return;
            }

            List<XNode> children = element.Nodes().ToList();
            foreach (var child in children.OfType<XElement>())
            {
                if (string.Equals(child.Name.LocalName, _inheritdoc,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Type baseType = member.DeclaringType.GetTypeInfo().BaseType;
                    MemberInfo baseMember =
                        baseType?.GetTypeInfo().DeclaredMembers
                            .SingleOrDefault(m => m.Name == member.Name);
                    if (baseMember != null)
                    {
                        var baseDoc = baseMember.GetXmlDocumentation();
                        if (baseDoc != null)
                        {
                            var nodes = baseDoc.Nodes()
                                .OfType<object>().ToArray();
                            child.ReplaceWith(nodes);
                        }
                        else
                        {
                            ProcessInheritdocInterfaceElements(member, child);
                        }
                    }
                    else
                    {
                        ProcessInheritdocInterfaceElements(member, child);
                    }
                }
            }
        }

        private static void ProcessInheritdocInterfaceElements(
            this MemberInfo member,
            XElement child)
        {
            foreach (Type baseInterface in member.DeclaringType
                .GetTypeInfo().ImplementedInterfaces)
            {
                MemberInfo baseMember = baseInterface?.GetTypeInfo()
                    .DeclaredMembers.SingleOrDefault(m =>
                        m.Name.EqualsOrdinal(member.Name));
                if (baseMember != null)
                {
                    XElement baseDoc = baseMember.GetXmlDocumentation();
                    if (baseDoc != null)
                    {
                        var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                        child.ReplaceWith(nodes);
                    }
                }
            }
        }

        // TODO : MST we have a much more efficient functionallity in our parser ... we should user ours instead
        private static string RemoveLineBreakWhiteSpaces(string documentation)
        {
            if (string.IsNullOrEmpty(documentation))
            {
                return string.Empty;
            }

            documentation = "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

            var whitespace = Regex.Match(documentation, "(\\n[ \\t]*)").Value;
            documentation = documentation.Replace(whitespace, "\n");

            return documentation.Trim('\n');
        }

        /// <exception cref="ArgumentException">Unknown member type.</exception>
        private static MemberName GetMemberElementName(MemberInfo member)
        {
            char prefixCode;

            var memberName = member is Type memberType && !string.IsNullOrEmpty(memberType.FullName) ?
                memberType.FullName :
                member.DeclaringType.FullName + "." + member.Name;

            switch (member.MemberType.ToString())
            {
                case "Constructor":
                    memberName = memberName.Replace(".ctor", "#ctor");
                    goto case "Method";

                case "Method":
                    prefixCode = 'M';

                    var paramTypesList = string.Join(",", ((MethodBase)member).GetParameters()
                        .Select(x => Regex
                            .Replace(x.ParameterType.FullName, "(`[0-9]+)|(, .*?PublicKeyToken=[0-9a-z]*)", string.Empty)
                            .Replace("[[", "{")
                            .Replace("]]", "}"))
                        .ToArray());

                    if (!string.IsNullOrEmpty(paramTypesList))
                    {
                        memberName += "(" + paramTypesList + ")";
                    }
                    break;

                case "Event":
                    prefixCode = 'E';
                    break;

                case "Field":
                    prefixCode = 'F';
                    break;

                case "NestedType":
                    memberName = memberName.Replace('+', '.');
                    goto case "TypeInfo";

                case "TypeInfo":
                    prefixCode = 'T';
                    break;

                case "Property":
                    prefixCode = 'P';
                    break;

                default:
                    throw new ArgumentException("Unknown member type.", "member");
            }
            return $"{prefixCode}:{memberName.Replace("+", ".")}";
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

        private ref struct MemberName
        {
            private const string _getMemberDocPath =
                "/doc/members/member[@name='{0}']";
            private const string _returnsPath = "{0}/returns";
            private const string _paramsPath = "{0}/param[@name='{1}']";

            public MemberName(string name)
            {
                Value = name;
                Path = string.Format(
                    CultureInfo.InvariantCulture,
                    _getMemberDocPath,
                    name);
                ReturnsPath = string.Format(
                    CultureInfo.InvariantCulture,
                    _returnsPath,
                    Path);
            }

            public string Value { get; }

            public string Path { get; }

            public string ReturnsPath { get; }

            public string GetParameterPath(string name)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    _paramsPath,
                    Path,
                    name);
            }

        }
    }
}
