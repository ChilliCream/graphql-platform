using System.Net.Http;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class XmlDocumentationProvider
        : IDocumentationProvider
    {
        private const string _summaryElementName = "summary";
        private const string _inheritdoc = "inheritdoc";
        private const string _see = "see";
        private const string _langword = "langword";
        private const string _cref = "cref";
        private const string _href = "href";

        private readonly IXmlDocumentationFileResolver _fileResolver;

        public XmlDocumentationProvider(
            IXmlDocumentationFileResolver fileResolver)
        {
            if (fileResolver == null)
            {
                throw new ArgumentNullException(nameof(fileResolver));
            }
            _fileResolver = fileResolver;
        }

        public string GetSummary(Type type) =>
            GetSummary((MemberInfo)type);

        public string GetSummary(MemberInfo member)
        {
            var assemblyName = member.Module.Assembly.GetName();
            var element = GetMemberElement(member);
            return RemoveLineBreakWhiteSpaces(
                GetText(element?.Element(_summaryElementName)));
        }

        public string GetSummary(ParameterInfo parameter)
        {
            var assemblyName = parameter.Member.Module.Assembly.GetName();
            var element = GetParameterElement(parameter);
            return RemoveLineBreakWhiteSpaces(
                GetText(element));
        }

        private static string GetText(XElement element)
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

                if (currentElement.Name != _see)
                {
                    value.Append(currentElement.Value);
                    continue;
                }

                var attribute = currentElement.Attribute(_langword);
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
                    attribute = currentElement.Attribute(_cref);
                    if (attribute != null)
                    {
                        value.Append(attribute.Value
                            .Trim('!', ':').Trim()
                            .Split('.').Last());
                    }
                    else
                    {
                        attribute = currentElement.Attribute(_href);
                        if (attribute != null)
                        {
                            value.Append(attribute.Value);
                        }
                    }
                }
            }

            return value.ToString();
        }

        private XElement GetMemberElement(MemberInfo member)
        {
            try
            {
                if (_fileResolver.TryGetXmlDocument(
                    member.Module.Assembly,
                    out XDocument document))
                {
                    MemberName name = GetMemberElementName(member);
                    XElement element = document.XPathSelectElements(name.Path)
                        .FirstOrDefault();

                    ReplaceInheritdocElements(member, element);

                    return element;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private XElement GetParameterElement(ParameterInfo parameter)
        {
            try
            {
                if (_fileResolver.TryGetXmlDocument(
                    parameter.Member.Module.Assembly,
                    out XDocument document))
                {
                    MemberName name = GetMemberElementName(parameter.Member);
                    var result = document.XPathSelectElements(name.Path);

                    var element = result.FirstOrDefault();
                    if (element == null)
                    {
                        return null;
                    }

                    ReplaceInheritdocElements(parameter.Member, element);

                    if (parameter.IsRetval
                        || string.IsNullOrEmpty(parameter.Name))
                    {
                        result = document.XPathSelectElements(name.ReturnsPath);
                    }
                    else
                    {
                        result = document.XPathSelectElements(
                            name.GetParameterPath(parameter.Name));
                    }

                    return result.FirstOrDefault();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void ReplaceInheritdocElements(
            MemberInfo member,
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
                        var baseDoc = GetMemberElement(baseMember);
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

        private void ProcessInheritdocInterfaceElements(
            MemberInfo member,
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
                    XElement baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                        child.ReplaceWith(nodes);
                    }
                }
            }
        }

        private static string RemoveLineBreakWhiteSpaces(string documentation)
        {
            if (string.IsNullOrEmpty(documentation))
            {
                return null;
            }

            documentation = "\n" + documentation
                .Replace("\r", string.Empty).Trim('\n');

            var whitespace = Regex.Match(documentation, "(\\n[ \\t]*)").Value;
            documentation = documentation.Replace(whitespace, "\n");

            return documentation.Trim('\n').Trim();
        }

        private static MemberName GetMemberElementName(MemberInfo member)
        {
            char prefixCode;

            var memberName = member is Type memberType
                && !string.IsNullOrEmpty(memberType.FullName)
                ? memberType.FullName
                : member.DeclaringType.FullName + "." + member.Name;

            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    memberName = memberName.Replace(".ctor", "#ctor");
                    goto case MemberTypes.Method;

                case MemberTypes.Method:
                    prefixCode = 'M';

                    var paramTypesList = string.Join(",",
                        ((MethodBase)member).GetParameters()
                        .Select(x => Regex
                            .Replace(x.ParameterType.FullName,
                                "(`[0-9]+)|(, .*?PublicKeyToken=[0-9a-z]*)",
                                string.Empty)
                            .Replace("[[", "{")
                            .Replace("]]", "}"))
                        .ToArray());

                    if (!string.IsNullOrEmpty(paramTypesList))
                    {
                        memberName += "(" + paramTypesList + ")";
                    }
                    break;

                case MemberTypes.Event:
                    prefixCode = 'E';
                    break;

                case MemberTypes.Field:
                    prefixCode = 'F';
                    break;

                case MemberTypes.NestedType:
                    memberName = memberName.Replace('+', '.');
                    goto case MemberTypes.TypeInfo;

                case MemberTypes.TypeInfo:
                    prefixCode = 'T';
                    break;

                case MemberTypes.Property:
                    prefixCode = 'P';
                    break;

                default:
                    throw new ArgumentException(
                        "Unknown member type.",
                        nameof(member));
            }

            return new MemberName(
                $"{prefixCode}:{memberName.Replace("+", ".")}");
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

    internal class NoopDocumentationProvider
        : IDocumentationProvider
    {
        public string GetSummary(Type type) => null;

        public string GetSummary(MemberInfo member) => null;

        public string GetSummary(ParameterInfo parameter) => null;
    }
}
