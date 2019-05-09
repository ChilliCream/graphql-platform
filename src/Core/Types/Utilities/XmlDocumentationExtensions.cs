using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    /// <remarks>This class currently works only on the desktop .NET framework.</remarks>
    internal static class XmlDocumentationExtensions
    {
        private static readonly AsyncLock Lock = new AsyncLock();
        private static readonly Dictionary<string, XDocument> Cache = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static Task<string> GetXmlSummaryAsync(this Type type)
        {
            return GetXmlDocumentationTagAsync(type.GetTypeInfo(), "summary");
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static Task<string> GetXmlRemarksAsync(this Type type)
        {
            return GetXmlDocumentationTagAsync(type.GetTypeInfo(), "remarks");
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static Task<string> GetXmlDocumentationTagAsync(this Type type, string tagName)
        {
            return GetXmlDocumentationTagAsync((MemberInfo)type.GetTypeInfo(), tagName);
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<string> GetXmlSummaryAsync(this MemberInfo member)
        {
            return await GetXmlDocumentationTagAsync(member, "summary").ConfigureAwait(false);
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<string> GetXmlRemarksAsync(this MemberInfo member)
        {
            return await GetXmlDocumentationTagAsync(member, "remarks").ConfigureAwait(false);
        }

        /// <summary>Converts the given XML documentation <see cref="XElement"/> to text.</summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The text</returns>
        internal static string GetXmlDocumentationText(this XElement element)
        {
            if (element == null)
            {
                return null;
            }

            var value = new StringBuilder();
            foreach (var node in element.Nodes())
            {
                if (node is XElement e)
                {
                    if (e.Name == "see")
                    {
                        var attribute = e.Attribute("langword");
                        if (attribute != null)
                        {
                            value.Append(attribute.Value);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(e.Value))
                            {
                                value.Append(e.Value);
                            }
                            else
                            {
                                attribute = e.Attribute("cref");
                                if (attribute != null)
                                {
                                    value.Append(attribute.Value.Trim('!', ':').Trim().Split('.').Last());
                                }
                                else
                                {
                                    attribute = e.Attribute("href");
                                    if (attribute != null)
                                    {
                                        value.Append(attribute.Value);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        value.Append(e.Value);
                    }
                }
                else
                {
                    value.Append(node);
                }
            }

            return value.ToString();
        }

        /// <summary>Clears the cache.</summary>
        /// <returns>The task.</returns>
        internal static Task ClearCacheAsync()
        {
            using (Lock.Lock())
            {
                Cache.Clear();
                return Task.CompletedTask;
            }
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<string> GetXmlDocumentationTagAsync(this MemberInfo member, string tagName)
        {
            var assemblyName = member.Module.Assembly.GetName();
            using (Lock.Lock())
            {
                if (IgnoreAssembly(assemblyName))
                {
                    return string.Empty;
                }

                var documentationPath = await GetXmlDocumentationPathAsync(member.Module.Assembly).ConfigureAwait(false);
                var element = await GetXmlDocumentationWithoutLockAsync(member, documentationPath).ConfigureAwait(false);
                return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element?.Element(tagName)));
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        internal static async Task<string> GetXmlDocumentationAsync(this ParameterInfo parameter)
        {
            var assemblyName = parameter.Member.Module.Assembly.GetName();
            using (Lock.Lock())
            {
                if (IgnoreAssembly(assemblyName))
                    return string.Empty;

                var documentationPath = await GetXmlDocumentationPathAsync(parameter.Member.Module.Assembly).ConfigureAwait(false);
                var element = await GetXmlDocumentationWithoutLockAsync(parameter, documentationPath).ConfigureAwait(false);
                return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element));
            }
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<XElement> GetXmlDocumentationAsync(this Type type, string pathToXmlFile)
        {
            using (Lock.Lock())
            {
                return await ((MemberInfo)type.GetTypeInfo()).GetXmlDocumentationWithoutLockAsync(pathToXmlFile).ConfigureAwait(false);
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        internal static async Task<XElement> GetXmlDocumentationAsync(this ParameterInfo parameter, string pathToXmlFile)
        {
            using (Lock.Lock())
            {
                return await GetXmlDocumentationWithoutLockAsync(parameter, pathToXmlFile).ConfigureAwait(false);
            }
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<XElement> GetXmlDocumentationAsync(this MemberInfo member)
        {
            using (Lock.Lock())
            {
                return await GetXmlDocumentationWithoutLockAsync(member).ConfigureAwait(false);
            }
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        internal static async Task<XElement> GetXmlDocumentationAsync(this MemberInfo member, string pathToXmlFile)
        {
            using (Lock.Lock())
            {
                return await GetXmlDocumentationWithoutLockAsync(member, pathToXmlFile).ConfigureAwait(false);
            }
        }

        private static async Task<XElement> GetXmlDocumentationWithoutLockAsync(this ParameterInfo parameter, string pathToXmlFile)
        {
            try
            {
                var assemblyName = parameter.Member.Module.Assembly.GetName();
                var document = await TryGetXmlDocumentAsync(assemblyName, pathToXmlFile).ConfigureAwait(false);
                if (document == null)
                {
                    return null;
                }

                return await GetXmlDocumentationAsync(parameter, document).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<XElement> GetXmlDocumentationWithoutLockAsync(this MemberInfo member)
        {
            var assemblyName = member.Module.Assembly.GetName();
            if (IgnoreAssembly(assemblyName))
            {
                return null;
            }

            var documentationPath = await GetXmlDocumentationPathAsync(member.Module.Assembly).ConfigureAwait(false);
            return await GetXmlDocumentationWithoutLockAsync(member, documentationPath).ConfigureAwait(false);
        }

        private static async Task<XElement> GetXmlDocumentationWithoutLockAsync(this MemberInfo member, string pathToXmlFile)
        {
            try
            {
                var assemblyName = member.Module.Assembly.GetName();
                var document = await TryGetXmlDocumentAsync(assemblyName, pathToXmlFile).ConfigureAwait(false);
                if (document == null)
                {
                    return null;
                }

                var element = GetXmlDocumentation(member, document);
                await ReplaceInheritdocElementsAsync(member, element).ConfigureAwait(false);
                return element;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<XDocument> TryGetXmlDocumentAsync(AssemblyName assemblyName, string pathToXmlFile)
        {
            if (!Cache.ContainsKey(assemblyName.FullName))
            {
                if (!File.Exists(pathToXmlFile))
                {
                    Cache[assemblyName.FullName] = null;
                    return null;
                }

                Cache[assemblyName.FullName] = await Task.Factory.StartNew(() => XDocument.Load(pathToXmlFile, LoadOptions.PreserveWhitespace)).ConfigureAwait(false);
            }

            return Cache[assemblyName.FullName];
        }

        private static bool IgnoreAssembly(AssemblyName assemblyName)
        {
            if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
            {
                return true;
            }

            return false;
        }

        private static XElement GetXmlDocumentation(this MemberInfo member, XDocument xml)
        {
            var name = GetMemberElementName(member);
            return xml.XPathSelectElements($"/doc/members/member[@name='{name}']")
                .FirstOrDefault();
        }

        private static async Task<XElement> GetXmlDocumentationAsync(this ParameterInfo parameter, XDocument xml)
        {
            var name = GetMemberElementName(parameter.Member);
            var result = xml.XPathSelectElements($"/doc/members/member[@name='{name}']");

            var element = result.FirstOrDefault();
            if (element != null)
            {
                await ReplaceInheritdocElementsAsync(parameter.Member, element).ConfigureAwait(false);

                if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
                    result = (IEnumerable)DynamicApis.XPathEvaluate(xml, $"/doc/members/member[@name='{name}']/returns");
                else
                    result = (IEnumerable)DynamicApis.XPathEvaluate(xml, $"/doc/members/member[@name='{name}']/param[@name='{parameter.Name}']");

                return result.OfType<XElement>().FirstOrDefault();
            }

            return null;
        }

        private static async Task ReplaceInheritdocElementsAsync(this MemberInfo member, XElement element)
        {
#if !LEGACY
            if (element == null)
                return;

            var children = element.Nodes().ToList();
            foreach (var child in children.OfType<XElement>())
            {
                if (child.Name.LocalName.ToLowerInvariant() == "inheritdoc")
                {
                    var baseType = member.DeclaringType.GetTypeInfo().BaseType;
                    var baseMember = baseType?.GetTypeInfo().DeclaredMembers.SingleOrDefault(m => m.Name == member.Name);
                    if (baseMember != null)
                    {
                        var baseDoc = await baseMember.GetXmlDocumentationWithoutLockAsync().ConfigureAwait(false);
                        if (baseDoc != null)
                        {
                            var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                            child.ReplaceWith(nodes);
                        }
                        else
                        {
                            await ProcessInheritdocInterfaceElementsAsync(member, child).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await ProcessInheritdocInterfaceElementsAsync(member, child).ConfigureAwait(false);
                    }
                }
            }
#endif
        }

#if !LEGACY
        private static async Task ProcessInheritdocInterfaceElementsAsync(this MemberInfo member, XElement child)
        {
            foreach (var baseInterface in member.DeclaringType.GetTypeInfo().ImplementedInterfaces)
            {
                var baseMember = baseInterface?.GetTypeInfo().DeclaredMembers.SingleOrDefault(m => m.Name == member.Name);
                if (baseMember != null)
                {
                    var baseDoc = await baseMember.GetXmlDocumentationWithoutLockAsync().ConfigureAwait(false);
                    if (baseDoc != null)
                    {
                        var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                        child.ReplaceWith(nodes);
                    }
                }
            }
        }
#endif

        private static string RemoveLineBreakWhiteSpaces(string documentation)
        {
            if (string.IsNullOrEmpty(documentation))
                return string.Empty;

            documentation = "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

            var whitespace = Regex.Match(documentation, "(\\n[ \\t]*)").Value;
            documentation = documentation.Replace(whitespace, "\n");

            return documentation.Trim('\n');
        }

        /// <exception cref="ArgumentException">Unknown member type.</exception>
        private static string GetMemberElementName(dynamic member)
        {
            char prefixCode;

            var memberName = member is Type memberType && !string.IsNullOrEmpty(memberType.FullName) ?
                memberType.FullName :
                member.DeclaringType.FullName + "." + member.Name;

            switch ((string)member.MemberType.ToString())
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
                        memberName += "(" + paramTypesList + ")";
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
            return string.Format("{0}:{1}", prefixCode, memberName.Replace("+", "."));
        }

        private static async Task<string> GetXmlDocumentationPathAsync(dynamic assembly)
        {
            string path;
            try
            {
                if (assembly == null)
                    return null;

                var assemblyName = assembly.GetName();
                if (string.IsNullOrEmpty(assemblyName.Name))
                    return null;

                if (Cache.ContainsKey(assemblyName.FullName))
                    return null;

                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    var assemblyDirectory = DynamicApis.PathGetDirectoryName((string)assembly.Location);
                    path = DynamicApis.PathCombine(assemblyDirectory, (string)assemblyName.Name + ".xml");
                    if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                        return path;
                }

                if (ReflectionExtensions.HasProperty(assembly, "CodeBase"))
                {
                    var codeBase = (string)assembly.CodeBase;
                    if (!string.IsNullOrEmpty(codeBase))
                    {
                        path = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(codeBase
                            .Replace("file:///", string.Empty)), assemblyName.Name + ".xml")
                            .Replace("file:\\", string.Empty);

                        if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                            return path;
                    }
                }

                var currentDomain = Type.GetType("System.AppDomain")?.GetRuntimeProperty("CurrentDomain").GetValue(null);
                if (currentDomain?.HasProperty("BaseDirectory") == true)
                {
                    var baseDirectory = currentDomain.TryGetPropertyValue("BaseDirectory", "");
                    if (!string.IsNullOrEmpty(baseDirectory))
                    {
                        path = DynamicApis.PathCombine(baseDirectory, assemblyName.Name + ".xml");
                        if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                            return path;

                        return DynamicApis.PathCombine(baseDirectory, "bin\\" + assemblyName.Name + ".xml");
                    }
                }

                var currentDirectory = await DynamicApis.DirectoryGetCurrentDirectoryAsync();
                path = DynamicApis.PathCombine(currentDirectory, assembly.GetName().Name + ".xml");
                if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                {
                    return path;
                }

                path = DynamicApis.PathCombine(currentDirectory, "bin\\" + assembly.GetName().Name + ".xml");
                if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
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

        private class AsyncLock : IDisposable
        {
            private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

            public AsyncLock Lock()
            {
                _semaphoreSlim.Wait();
                return this;
            }

            public void Dispose()
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
