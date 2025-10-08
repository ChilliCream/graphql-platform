using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Types.Descriptors;

public partial class XmlDocumentationProvider : IDocumentationProvider
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

    private static readonly XPathExpression s_summaryXPath = XPathExpression.Compile(SummaryElementName);
    private static readonly XPathExpression s_returnsXPath = XPathExpression.Compile(ReturnsElementName);
    private static readonly XPathExpression s_exceptionXPath = XPathExpression.Compile(ExceptionElementName);
    private static readonly XPathExpression s_inheritDocXPath = XPathExpression.Compile($".//*[local-name()='{Inheritdoc}']");

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

        var description = _stringBuilderPool.Get();
        try
        {
            AppendText(element, description);

            if (description.Length == 0)
            {
                return null;
            }

            return RemoveLineBreakWhiteSpaces(description);
        }
        finally
        {
            _stringBuilderPool.Return(description);
        }
    }

    private string? GetDescriptionInternal(MemberInfo member)
    {
        var element = GetMemberElement(member);

        if (element is null)
        {
            return null;
        }

        var summaryNode = element.SelectSingleNode(s_summaryXPath);
        var returnsNode = element.SelectSingleNode(s_returnsXPath);
        var exceptionNodes = element.Select(s_exceptionXPath);

        return ComposeMemberDescription(
            summaryNode,
            returnsNode,
            exceptionNodes);
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

            return RemoveLineBreakWhiteSpaces(description);
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
                var element = document.SelectSingleNode(name.Path);
                if (element == null)
                {
                    return null;
                }

                element = ReplaceInheritdocElements(member, element);

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
                var result = document.SelectSingleNode(name.Path);

                if (result is null)
                {
                    return null;
                }

                result = ReplaceInheritdocElements(parameter.Member, result);

                if (parameter.IsRetval
                    || string.IsNullOrEmpty(parameter.Name))
                {
                    result = result.SelectSingleNode(MemberName.RelativeReturnsPath);
                }
                else
                {
                    result = result.SelectSingleNode(
                        name.GetRelativeParameterPath(parameter.Name));
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

    private void ProcessInheritdocInterfaceElements(
        MemberInfo member,
        XElement child)
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
                        var nodes = XElement.Parse(baseDoc.OuterXml, LoadOptions.PreserveWhitespace).Nodes().OfType<object>().ToArray();
                        child.ReplaceWith(nodes);
                    }
                }
            }
        }
    }

       private XPathNavigator ReplaceInheritdocElements(
        MemberInfo member,
        XPathNavigator element)
    {
        if (member.DeclaringType?.BaseType is null || !element.InnerXml.Contains(Inheritdoc, StringComparison.Ordinal))
        {
            return element;
        }

        XElement xElement = XElement.Parse(element.OuterXml, LoadOptions.PreserveWhitespace);
        var inheritDocNodes = xElement.XPathSelectElements($"//*[local-name()='{Inheritdoc}']");

        foreach (var child in inheritDocNodes)
        {
            var baseMember =
                member.DeclaringType.BaseType
                    .GetTypeInfo().DeclaredMembers
                    .SingleOrDefault(m => m.Name == member.Name);

            if (baseMember != null)
            {
                var baseDoc = GetMemberElement(baseMember);
                if (baseDoc != null)
                {
                    var nodes = XElement.Parse(baseDoc.OuterXml, LoadOptions.PreserveWhitespace).Nodes().OfType<object>().ToArray();
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

        return xElement.CreateNavigator();
    }

    private static string? RemoveLineBreakWhiteSpaces(StringBuilder stringBuilder)
    {
        if (stringBuilder.Length == 0)
        {
            return null;
        }

        stringBuilder.Replace("\r", string.Empty);
        if (stringBuilder[0] != '\n')
        {
            stringBuilder.Insert(0, '\n');
        }

        if (stringBuilder[^1] == '\n')
        {
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
        }

        var materializedString = stringBuilder.ToString();
        var whitespace = MyRegex().Match(materializedString).Value;
        if (!string.IsNullOrEmpty(whitespace))
        {
            stringBuilder.Replace(whitespace, "\n");
            materializedString = stringBuilder.ToString();
        }

        return materializedString.Trim('\n', ' ');
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
        private const string ParamsPathFormat = "./param[@name='{0}']";
        internal const string RelativeReturnsPath = "./returns";

        public MemberName(string name)
        {
            Path = string.Format(
                CultureInfo.InvariantCulture,
                GetMemberDocPathFormat,
                name);
        }

        public string Path { get; }

        public string GetRelativeParameterPath(string name)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ParamsPathFormat,
                name);
        }
    }

    [GeneratedRegex("(\\n[ \\t]*)")]
    private static partial Regex MyRegex();
}
