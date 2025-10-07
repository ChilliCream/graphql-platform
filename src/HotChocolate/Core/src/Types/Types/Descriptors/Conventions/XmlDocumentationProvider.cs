using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationProvider : IDocumentationProvider
{
    private const string SummaryElementName = "summary";
    private const string ExceptionElementName = "exception";
    private const string ReturnsElementName = "returns";
    private const string Inheritdoc = "inheritdoc";
    private const string See = "see";
    private const string Langword = "langword";
    private const string Cref = "cref";
    private const string Href = "href";
    private const string Code = "code";
    private const string Paramref = "paramref";
    private const string Name = "name";

    private readonly IXmlDocumentationFileResolver _fileResolver;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public XmlDocumentationProvider(
        IXmlDocumentationFileResolver fileResolver,
        ObjectPool<StringBuilder> stringBuilderPool)
    {
        _fileResolver = fileResolver ?? throw new ArgumentNullException(nameof(fileResolver));
        _stringBuilderPool = stringBuilderPool;
    }

    public string? GetDescription(Type type) =>
        GetDescriptionInternal(type);

    public string? GetDescription(MemberInfo member) =>
        GetDescriptionInternal(member);

    public string? GetDescription(ParameterInfo parameter)
    {
        var element = GetParameterElement(parameter);

        if (element is null)
        {
            return null;
        }

        var description = new StringBuilder();
        AppendText(element, description);

        if (description.Length == 0)
        {
            return null;
        }

        return RemoveLineBreakWhiteSpaces(description.ToString());
    }

    private string? GetDescriptionInternal(MemberInfo member)
    {
        var element = GetMemberElement(member);

        if (element is null)
        {
            return null;
        }

        var summaryNode = element.SelectSingleNode(SummaryElementName);
        var returnsNode = element.SelectSingleNode(ReturnsElementName);
        var exceptionNodes = element.Select(ExceptionElementName);

        var description = ComposeMemberDescription(
            summaryNode,
            returnsNode,
            exceptionNodes);

        return RemoveLineBreakWhiteSpaces(description);
    }

    private string? ComposeMemberDescription(
        XPathNavigator? summary,
        XPathNavigator? returns,
        XPathNodeIterator errors)
    {
        var description = _stringBuilderPool.Get();

        try
        {
            var needsNewLine = false;

            if (!string.IsNullOrEmpty(summary?.Value))
            {
                AppendText(summary, description);
                needsNewLine = true;
            }

            if (!string.IsNullOrEmpty(returns?.Value))
            {
                AppendNewLineIfNeeded(description, needsNewLine);
                description.AppendLine("**Returns:**");
                AppendText(returns, description);
                needsNewLine = true;
            }

            AppendErrorDescription(errors, description, needsNewLine);

            return description.Length == 0 ? null : description.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(description);
        }
    }

    private void AppendErrorDescription(
        XPathNodeIterator errors,
        StringBuilder description,
        bool needsNewLine)
    {
        var errorCount = 0;
        while (errors.MoveNext())
        {
            var error = errors.Current;
            if(string.IsNullOrEmpty(error?.Value))
            {
                continue;
            }

            var code = error.GetAttribute(Code, string.Empty);
            if (!string.IsNullOrEmpty(code))
            {
                if (errorCount == 0)
                {
                    AppendNewLineIfNeeded(description, needsNewLine);
                    description.AppendLine("**Errors:**");
                }
                else
                {
                    description.AppendLine();
                }

                description.Append($"{++errorCount}. ");
                description.Append($"{code}: ");

                AppendText(error, description);
            }
        }
    }

    private static void AppendText(
        XPathNavigator? element,
        StringBuilder description)
    {
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
        {
            return;
        }

        var children = element.SelectChildren(XPathNodeType.All);
        while (children.MoveNext())
        {
            var child = children.Current;
            switch (child?.NodeType)
            {
                case XPathNodeType.Text:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.Whitespace:
                    description.Append(child.Value);
                    break;

                case XPathNodeType.Element:
                    var localName = child.LocalName;

                    if (localName == Paramref)
                    {
                        var nameAttr = child.GetAttribute(Name, string.Empty);
                        description.Append(nameAttr);
                        break;
                    }

                    if (localName != See)
                    {
                        description.Append(child.Value);
                        break;
                    }

                    // handle <see ... />
                    var langword = child.GetAttribute(Langword, string.Empty);
                    if (!string.IsNullOrEmpty(langword))
                    {
                        description.Append(langword);
                        break;
                    }

                    if (!string.IsNullOrEmpty(child.Value))
                    {
                        description.Append(child.Value);
                        break;
                    }

                    var cref = child.GetAttribute(Cref, string.Empty);
                    if (!string.IsNullOrEmpty(cref))
                    {
                        // TODO
                        description.Append(
                            cref.Trim('!', ':', ' ')
                                .Split('.').Last());
                        break;
                    }

                    var href = child.GetAttribute(Href, string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        description.Append(href);
                    }

                    break;
            }
        }
    }

    private void AppendNewLineIfNeeded(
        StringBuilder description,
        bool condition)
    {
        if (condition)
        {
            description.AppendLine();
            description.AppendLine();
        }
    }

    private XPathNavigator? GetMemberElement(MemberInfo member)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(member);
                var element = document.CreateNavigator().SelectSingleNode(name.Path);
                if (element == null)
                {
                    return null;
                }

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

    private XPathNavigator? GetParameterElement(ParameterInfo parameter)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                parameter.Member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(parameter.Member);
                var navigator = document.CreateNavigator();
                var result = navigator.SelectSingleNode(name.Path);

                if (result is null)
                {
                    return null;
                }

                ReplaceInheritdocElements(parameter.Member, result);

                if (parameter.IsRetval
                    || string.IsNullOrEmpty(parameter.Name))
                {
                    result = navigator.SelectSingleNode(name.ReturnsPath);
                }
                else
                {
                    result = navigator.SelectSingleNode(
                        name.GetParameterPath(parameter.Name));
                }

                return result;
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
        XPathNavigator element)
    {
        var interitDocChildren = element.SelectChildren(Inheritdoc, string.Empty);
        while (interitDocChildren.MoveNext())
        {
            var child = interitDocChildren.Current;
            if (child is null)
            {
                continue;
            }

            if (string.Equals(child.LocalName, Inheritdoc,
               StringComparison.OrdinalIgnoreCase))
            {
                var baseType =
                    member.DeclaringType?.GetTypeInfo().BaseType;
                var baseMember =
                    baseType?.GetTypeInfo().DeclaredMembers
                        .SingleOrDefault(m => m.Name == member.Name);

                if (baseMember != null)
                {
                    var baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        child.ReplaceSelf(baseDoc.InnerXml);
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
        XPathNavigator child)
    {
        if (member.DeclaringType is { })
        {
            foreach (var baseInterface in member.DeclaringType
                .GetTypeInfo().ImplementedInterfaces)
            {
                var baseMember = baseInterface.GetTypeInfo()
                    .DeclaredMembers.SingleOrDefault(m =>
                        m.Name.EqualsOrdinal(member.Name));
                if (baseMember != null)
                {
                    var baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        child.ReplaceSelf(baseDoc.InnerXml);
                        return;
                    }
                }
            }
        }
    }

    private static string? RemoveLineBreakWhiteSpaces(string? documentation)
    {
        if (string.IsNullOrWhiteSpace(documentation))
        {
            return null;
        }

        documentation =
            "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

        var whitespace =
            Regex.Match(documentation, "(\\n[ \\t]*)").Value;

        documentation = documentation.Replace(whitespace, "\n");

        return documentation.Trim('\n').Trim();
    }

    private static MemberName GetMemberElementName(MemberInfo member)
    {
        char prefixCode;

        var memberName =
            member is Type { FullName: { Length: > 0 } } memberType
            ? memberType.FullName
            : member.DeclaringType is null
                ? member.Name
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
                        .Replace(x.ParameterType.FullName!,
                            "(`[0-9]+)|(, .*?PublicKeyToken=[0-9a-z]*)",
                            string.Empty)
                        .Replace("[[", "{")
                        .Replace("]]", "}")
                        .Replace("],[", ","))
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
                memberName = memberName?.Replace('+', '.');
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
            $"{prefixCode}:{memberName?.Replace("+", ".")}");
    }

    private ref struct MemberName
    {
        private const string GetMemberDocPathFormat = "/doc/members/member[@name='{0}']";
        private const string ReturnsPathFormat = "{0}/returns";
        private const string ParamsPathFormat = "{0}/param[@name='{1}']";

        public MemberName(string name)
        {
            Value = name;
            Path = string.Format(
                CultureInfo.InvariantCulture,
                GetMemberDocPathFormat,
                name);
            ReturnsPath = string.Format(
                CultureInfo.InvariantCulture,
                ReturnsPathFormat,
                Path);
        }

        public string Value { get; }

        public string Path { get; }

        public string ReturnsPath { get; }

        public string GetParameterPath(string name)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ParamsPathFormat,
                Path,
                name);
        }
    }
}
