using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationProvider : IDocumentationProvider
{
    private const string _summaryElementName = "summary";
    private const string _exceptionElementName = "exception";
    private const string _returnsElementName = "returns";
    private const string _inheritdoc = "inheritdoc";
    private const string _see = "see";
    private const string _langword = "langword";
    private const string _cref = "cref";
    private const string _href = "href";
    private const string _code = "code";
    private const string _paramref = "paramref";
    private const string _name = "name";

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

        var description = ComposeMemberDescription(
            element.Element(_summaryElementName),
            element.Element(_returnsElementName),
            element.Elements(_exceptionElementName));

        return RemoveLineBreakWhiteSpaces(description);
    }

    private string? ComposeMemberDescription(
        XElement? summary,
        XElement? returns,
        IEnumerable<XElement> errors)
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
        IEnumerable<XElement> errors,
        StringBuilder description,
        bool needsNewLine)
    {
        var errorCount = 0;
        foreach (var error in errors)
        {
            var code = error.Attribute(_code);
            if (code is { }
                && !string.IsNullOrEmpty(error.Value)
                && !string.IsNullOrEmpty(code.Value))
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
                description.Append($"{code.Value}: ");

                AppendText(error, description);
            }
        }
    }

    private static void AppendText(
        XElement? element,
        StringBuilder description)
    {
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
        {
            return;
        }

        foreach (var node in element.Nodes())
        {
            if (node is not XElement currentElement)
            {
                if (node is XText text)
                {
                    description.Append(text.Value);
                }

                continue;
            }

            if (currentElement.Name == _paramref)
            {
                var nameAttribute = currentElement.Attribute(_name);

                if (nameAttribute != null)
                {
                    description.Append(nameAttribute.Value);
                    continue;
                }
            }

            if (currentElement.Name != _see)
            {
                description.Append(currentElement.Value);
                continue;
            }

            var attribute = currentElement.Attribute(_langword);
            if (attribute != null)
            {
                description.Append(attribute.Value);
                continue;
            }

            if (!string.IsNullOrEmpty(currentElement.Value))
            {
                description.Append(currentElement.Value);
            }
            else
            {
                attribute = currentElement.Attribute(_cref);
                if (attribute != null)
                {
                    description.Append(attribute.Value
                        .Trim('!', ':').Trim()
                        .Split('.').Last());
                }
                else
                {
                    attribute = currentElement.Attribute(_href);
                    if (attribute != null)
                    {
                        description.Append(attribute.Value);
                    }
                }
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

    private XElement? GetMemberElement(MemberInfo member)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(member);
                var element = document.XPathSelectElements(name.Path)
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

    private XElement? GetParameterElement(ParameterInfo parameter)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                parameter.Member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(parameter.Member);
                var result = document.XPathSelectElements(name.Path);
                var element = result.FirstOrDefault();

                if (element is null)
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
        XElement? element)
    {
        if (element is null)
        {
            return;
        }

        var children = element.Nodes().ToList();
        foreach (var child in children.OfType<XElement>())
        {
            if (string.Equals(child.Name.LocalName, _inheritdoc,
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
                        var nodes =
                            baseDoc.Nodes().OfType<object>().ToArray();
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
                        child.ReplaceWith(
                            baseDoc.Nodes().OfType<object>().ToArray());
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
            member is Type { FullName: { Length: > 0, }, } memberType
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
