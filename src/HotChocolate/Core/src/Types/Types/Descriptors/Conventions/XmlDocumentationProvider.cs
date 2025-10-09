using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

        var summaryNode = element.Element(SummaryElementName);
        var returnsNode = element.Element(ReturnsElementName);
        var exceptionNodes = element.Descendants(ExceptionElementName);

        return ComposeMemberDescription(
            summaryNode,
            returnsNode,
            exceptionNodes);
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

            return description.Length == 0 ? null : RemoveLineBreakWhiteSpaces(description);
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
            if (!error.IsEmpty)
            {
                var code = error.Attribute(Code);
                if (code is not null)
                {
                    var codeValue = code.Value;
                    if (!string.IsNullOrEmpty(codeValue))
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
                        description.Append($"{codeValue}: ");

                        AppendText(error, description);
                    }
                }
            }
        }
    }

    private static void AppendText(
        XElement? element,
        StringBuilder description)
    {
        if (element?.IsEmpty != false)
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

            if (currentElement.Name == Paramref)
            {
                var nameAttribute = currentElement.Attribute(Name);
                if (nameAttribute != null)
                {
                    description.Append(nameAttribute.Value);
                    continue;
                }
            }

            if (currentElement.Name != See)
            {
                description.Append(currentElement.Value);
                continue;
            }

            var attribute = currentElement.Attribute(Langword);
            if (attribute != null)
            {
                description.Append(attribute.Value);
                continue;
            }

            if (!currentElement.IsEmpty)
            {
                description.Append(currentElement.Value);
            }
            else
            {
                attribute = currentElement.Attribute(Cref);
                if (attribute != null)
                {
                    var value = attribute.Value.AsSpan().Trim(['!', ':', ' ']);

                    var lastDotIndex = value.LastIndexOf('.');
                    if (lastDotIndex >= 0)
                    {
                        value = value[(lastDotIndex + 1)..];
                    }

                    description.Append(value);
                }
                else
                {
                    attribute = currentElement.Attribute(Href);
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
                var x = document
                    .Element("doc")?
                    .Element("members")?
                    .Elements("member");
                  var element  = x?.FirstOrDefault(m => m.Attribute("name")?.Value == name);
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

    private XElement? GetParameterElement(ParameterInfo parameter)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                parameter.Member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(parameter.Member);
                var x = document
                    .Element("doc")?
                    .Element("members")?
                    .Elements("member");
                var element  = x?.FirstOrDefault(m => m.Attribute("name")?.Value == name);

                if (element is null)
                {
                    return null;
                }

                ReplaceInheritdocElements(parameter.Member, element);

                if (parameter.IsRetval
                    || string.IsNullOrEmpty(parameter.Name))
                {
                    return element.Element("returns");
                }

                return element
                    .Elements("param")
                    .FirstOrDefault(m => m.Attribute("name")?.Value == parameter.Name);
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
            foreach (var baseInterface in member.DeclaringType.GetInterfaces())
            {
                var baseMember = baseInterface.GetTypeInfo()
                    .DeclaredMembers.SingleOrDefault(m =>
                        m.Name.EqualsOrdinal(member.Name));
                if (baseMember != null)
                {
                    var baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        // TODO: This implicitly mutates the document (which is cached!) and is not threadsafe -> potential flaw
                        child.ReplaceWith(baseDoc.Nodes());
                    }
                }
            }
        }
    }

    private void ReplaceInheritdocElements(
        MemberInfo member,
        XElement element)
    {
        var baseType = member.DeclaringType?.BaseType;
        if (baseType is null)
        {
            return;
        }

        var children = element.Elements(Inheritdoc);
        foreach (var child in children)
        {
            var baseMember =
                baseType?.GetTypeInfo().DeclaredMembers
                    .SingleOrDefault(m => m.Name == member.Name);

            if (baseMember != null)
            {
                var baseDoc = GetMemberElement(baseMember);
                if (baseDoc != null)
                {
                    // TODO: This implicitly mutates the document (which is cached!) and is not threadsafe -> potential flaw
                    var nodes = baseDoc.Nodes();
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

    private string GetMemberElementName(MemberInfo member)
    {
        char prefixCode;
        var builder = _stringBuilderPool.Get();
        try
        {
            if (member is Type { FullName.Length: > 0 } memberType)
            {
                builder.Append(memberType.FullName);
            }
            else if (member.DeclaringType is null)
            {
                builder.Append(member.Name);
            }
            else
            {
                builder.Append(member.DeclaringType.FullName).Append('.').Append(member.Name);
            }

            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    builder.Replace(".ctor", "#ctor");
                    goto case MemberTypes.Method;

                case MemberTypes.Method:
                    prefixCode = 'M';

                    var parameters = ((MethodBase)member).GetParameters();
                    if (parameters.Length > 0)
                    {
                        builder.Append('(');
                        for (var index = 0; index < parameters.Length; index++)
                        {
                            var parameterInfo = parameters[index];
                            var result = MyRegex1().Replace(parameterInfo.ParameterType.FullName!, m => m.Value switch
                            {
                                "[[" => "{",
                                "]]" => "}",
                                "],[" => ",",
                                _ => ""
                            });
                            builder.Append(result);
                            if (index < parameters.Length - 1)
                            {
                                builder.Append(',');
                            }
                        }

                        builder.Append(')');
                    }

                    break;

                case MemberTypes.Event:
                    prefixCode = 'E';
                    break;

                case MemberTypes.Field:
                    prefixCode = 'F';
                    break;

                case MemberTypes.NestedType:
                    builder.Replace('+', '.');
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

            return builder.Insert(0, prefixCode).Insert(1, ':').Replace("+", ".").ToString();
        }
        finally
        {
            _stringBuilderPool.Return(builder);
        }
    }

    [GeneratedRegex("(\\n[ \\t]*)")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"(`\d+)|(, .*?PublicKeyToken=[0-9a-z]*)|\[\[|\]\]|\],\[")]
    private static partial Regex MyRegex1();
}
